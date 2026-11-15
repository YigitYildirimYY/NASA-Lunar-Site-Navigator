"""
NASA Lunar Site Navigator — Web Application & Ephemeris API Server
==================================================================
Serves:
  - GET /                     -> Interactive 3D Lunar Mission Planner & Landing Simulator
  - GET /api/sites            -> Curated NASA Artemis, CLPS, Apollo & International sites + Data Safety Audit
  - GET /api/evaluate         -> Full JPL DE421 + LRO LOLA horizon, solar power, DTE & 708h timeline evaluation
  - GET /api/compare          -> Rapid Scenario A vs Scenario B trade-off comparison
  - GET /api/terrain_patch    -> Local LRO LOLA elevation & slope matrix for 3D curved planetary surface rendering
"""

import os
import sys
import json
import math
import urllib.parse
from http.server import HTTPServer, SimpleHTTPRequestHandler

from lunar_engine import (
    LunarTerrainEngine,
    CURATED_SITES,
    evaluate_site_and_timeline,
    R_MOON_KM,
    R_MOON_M,
)

BASE_DIR = os.path.dirname(os.path.abspath(__file__))
DATA_DIR = os.path.join(BASE_DIR, "data")
STATIC_DIR = os.path.join(BASE_DIR, "static")

TERRAIN_ENGINE = LunarTerrainEngine()
SITES_BY_ID = {s["id"]: s for s in CURATED_SITES}


def build_terrain_patch(lat_center: float, lon_center: float, span_km: float = 140.0, grid_n: int = 96):
    """
    Extracts a local (grid_n x grid_n) elevation (m) and slope (deg) grid around (lat_center, lon_center)
    directly from the real NASA LRO LOLA .npy Digital Elevation Model matrices (no random/synthetic craters).
    """
    half = span_km / 2.0
    elevs = []
    slopes = []
    lat0_rad = math.radians(lat_center)
    lon0_rad = math.radians(lon_center)

    # Sample grid of real LRO LOLA elevations
    raw_grid = [[0.0] * grid_n for _ in range(grid_n)]
    for iy in range(grid_n):
        ny = -half + (iy / (grid_n - 1)) * span_km  # North-South offset (km)
        for ix in range(grid_n):
            nx = -half + (ix / (grid_n - 1)) * span_km  # East-West offset (km)
            dist_km = math.hypot(nx, ny)
            if dist_km < 1e-6:
                lat_p, lon_p = lat_center, lon_center
            else:
                ang = dist_km / R_MOON_KM
                az = math.atan2(nx, ny)
                sin_lat = math.sin(lat0_rad) * math.cos(ang) + math.cos(lat0_rad) * math.sin(ang) * math.cos(az)
                sin_lat = max(-1.0, min(1.0, sin_lat))
                lat_p = math.degrees(math.asin(sin_lat))
                y = math.sin(az) * math.sin(ang) * math.cos(lat0_rad)
                x = math.cos(ang) - math.sin(lat0_rad) * sin_lat
                lon_p = math.degrees(lon0_rad + math.atan2(y, x))

            base_h = TERRAIN_ENGINE.sample_elevation_m(lat_p, lon_p)
            raw_grid[iy][ix] = round(base_h, 1)

    step_m = (span_km * 1000.0) / (grid_n - 1)
    for iy in range(grid_n):
        row_e = []
        row_s = []
        for ix in range(grid_n):
            y_prev = max(0, iy - 1)
            y_next = min(grid_n - 1, iy + 1)
            x_prev = max(0, ix - 1)
            x_next = min(grid_n - 1, ix + 1)
            dy_m = max(1.0, (y_next - y_prev) * step_m)
            dx_m = max(1.0, (x_next - x_prev) * step_m)
            dz_dy = (raw_grid[y_next][ix] - raw_grid[y_prev][ix]) / dy_m
            dz_dx = (raw_grid[iy][x_next] - raw_grid[iy][x_prev]) / dx_m
            slope_deg = round(math.degrees(math.atan(math.hypot(dz_dx, dz_dy))), 1)
            row_e.append(raw_grid[iy][ix])
            row_s.append(slope_deg)
        elevs.append(row_e)
        slopes.append(row_s)

    return {
        "lat_center": lat_center,
        "lon_center": lon_center,
        "span_km": span_km,
        "grid_n": grid_n,
        "lunar_radius_km": R_MOON_KM,
        "elevations_m": elevs,
        "slopes_deg": slopes,
    }


class LunarNavigatorHandler(SimpleHTTPRequestHandler):
    def log_message(self, format, *args):
        # Quiet clean logging
        pass

    def _send_json(self, data: dict, status: int = 200):
        body = json.dumps(data).encode("utf-8")
        self.send_response(status)
        self.send_header("Content-Type", "application/json; charset=utf-8")
        self.send_header("Content-Length", str(len(body)))
        self.send_header("Cache-Control", "no-store")
        self.end_headers()
        self.wfile.write(body)

    def do_GET(self):
        parsed = urllib.parse.urlparse(self.path)
        path = parsed.path
        qs = urllib.parse.parse_qs(parsed.query)

        if path == "/api/sites":
            de421_path = os.path.join(DATA_DIR, "de421.bsp")
            lola_global_path = os.path.join(DATA_DIR, "lola_global_dem.npy")
            lola_sp_path = os.path.join(DATA_DIR, "lola_south_pole_dem.npy")
            self._send_json(
                {
                    "sites": CURATED_SITES,
                    "data_safety_audit": {
                        "max_allowed_mb": 50.0,
                        "blocked_large_file": "ldem_16.tif (63.30 MB > 50 MB limit — automatically aborted)",
                        "de421_bsp_mb": round(os.path.getsize(de421_path) / (1024 * 1024), 2)
                        if os.path.exists(de421_path)
                        else 0,
                        "lola_global_npy_kb": round(os.path.getsize(lola_global_path) / 1024, 1)
                        if os.path.exists(lola_global_path)
                        else 0,
                        "lola_south_pole_npy_kb": round(os.path.getsize(lola_sp_path) / 1024, 1)
                        if os.path.exists(lola_sp_path)
                        else 0,
                        "de421_spk_active": TERRAIN_ENGINE.de421.loaded,
                    },
                }
            )
            return

        if path == "/api/evaluate":
            site_id = qs.get("site_id", [None])[0]
            site_obj = SITES_BY_ID.get(site_id) if site_id else None
            lat = float(qs.get("lat", [site_obj["lat"] if site_obj else -89.54])[0])
            lon = float(qs.get("lon", [site_obj["lon"] if site_obj else 0.0])[0])
            iso_time = qs.get("time", ["2026-11-15T12:00:00Z"])[0]
            window_hours = int(qs.get("window_hours", [708])[0])
            step_hours = int(qs.get("step_hours", [6])[0])

            override_elev = None
            override_slope = None
            site_name = qs.get("name", [None])[0]
            if site_obj and abs(lat - site_obj["lat"]) < 1e-3 and abs(lon - site_obj["lon"]) < 1e-3:
                override_elev = site_obj["elev_m"]
                override_slope = site_obj["slope_deg"]
                site_name = site_obj["name"]

            res = evaluate_site_and_timeline(
                TERRAIN_ENGINE,
                lat=lat,
                lon=lon,
                iso_time=iso_time,
                window_hours=window_hours,
                step_hours=step_hours,
                site_name=site_name,
                override_elev_m=override_elev,
                override_slope_deg=override_slope,
            )
            if site_obj:
                res["site"]["id"] = site_obj["id"]
                res["site"]["program"] = site_obj["program"]
                res["site"]["region"] = site_obj["region"]
                res["site"]["water_ice_potential"] = site_obj["water_ice_potential"]
                res["site"]["description"] = site_obj["description"]
            self._send_json(res)
            return

        if path == "/api/compare":
            site_a_id = qs.get("siteA", ["shackleton_rim"])[0]
            site_b_id = qs.get("siteB", ["malapert_massif"])[0]
            time_a = qs.get("timeA", ["2026-11-15T12:00:00Z"])[0]
            time_b = qs.get("timeB", [time_a])[0]

            sa = SITES_BY_ID.get(site_a_id)
            sb = SITES_BY_ID.get(site_b_id)
            lat_a = float(qs.get("latA", [sa["lat"] if sa else -89.54])[0])
            lon_a = float(qs.get("lonA", [sa["lon"] if sa else 0.0])[0])
            lat_b = float(qs.get("latB", [sb["lat"] if sb else -86.00])[0])
            lon_b = float(qs.get("lonB", [sb["lon"] if sb else 0.0])[0])

            eval_a = evaluate_site_and_timeline(
                TERRAIN_ENGINE,
                lat_a,
                lon_a,
                time_a,
                site_name=sa["name"] if sa else None,
                override_elev_m=sa["elev_m"] if sa else None,
                override_slope_deg=sa["slope_deg"] if sa else None,
            )
            eval_b = evaluate_site_and_timeline(
                TERRAIN_ENGINE,
                lat_b,
                lon_b,
                time_b,
                site_name=sb["name"] if sb else None,
                override_elev_m=sb["elev_m"] if sb else None,
                override_slope_deg=sb["slope_deg"] if sb else None,
            )
            self._send_json({"scenarioA": eval_a, "scenarioB": eval_b})
            return

        if path == "/api/terrain_patch":
            lat = float(qs.get("lat", [-89.54])[0])
            lon = float(qs.get("lon", [0.0])[0])
            span_km = float(qs.get("span_km", [90.0])[0])
            grid_n = min(128, max(24, int(qs.get("grid", [96])[0])))
            patch = build_terrain_patch(lat, lon, span_km, grid_n)
            self._send_json(patch)
            return

        # Serve index.html and static/data files
        if path == "/" or path == "/index.html":
            self.path = "/static/index.html"
        return super().do_GET()


def run_server(port: int = 8080):
    os.chdir(BASE_DIR)
    server = HTTPServer(("127.0.0.1", port), LunarNavigatorHandler)
    print(f"[SERVER] NASA Lunar Site Navigator running at http://localhost:{port}")
    sys.stdout.flush()
    server.serve_forever()


if __name__ == "__main__":
    port = int(sys.argv[1]) if len(sys.argv) > 1 else 8080
    run_server(port)
