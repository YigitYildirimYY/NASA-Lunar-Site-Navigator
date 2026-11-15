/**
 * Node.js Production Build Script for `npm run build`
 * Generates `dist/` and `build/` directories and verifies all asset paths.
 */
const fs = require("fs");
const path = require("path");

const BASE_DIR = __dirname;
const STATIC_DIR = path.join(BASE_DIR, "static");
const DIST_DIR = path.join(BASE_DIR, "dist");
const BUILD_DIR = path.join(BASE_DIR, "build");

function copyRecursiveSync(src, dest) {
  const exists = fs.existsSync(src);
  const stats = exists && fs.statSync(src);
  const isDirectory = exists && stats.isDirectory();
  if (isDirectory) {
    fs.mkdirSync(dest, { recursive: true });
    fs.readdirSync(src).forEach((childItemName) => {
      copyRecursiveSync(path.join(src, childItemName), path.join(dest, childItemName));
    });
  } else if (exists) {
    fs.mkdirSync(path.dirname(dest), { recursive: true });
    fs.copyFileSync(src, dest);
  }
}

function buildTarget(targetDir) {
  if (fs.existsSync(targetDir)) {
    fs.rmSync(targetDir, { recursive: true, force: true });
  }
  fs.mkdirSync(targetDir, { recursive: true });

  // 1. Copy index.html to root of targetDir
  fs.copyFileSync(path.join(STATIC_DIR, "index.html"), path.join(targetDir, "index.html"));

  // 2. Copy static/* to targetDir/static/*
  copyRecursiveSync(STATIC_DIR, path.join(targetDir, "static"));

  // 3. Root fallback copies
  fs.copyFileSync(path.join(STATIC_DIR, "styles.css"), path.join(targetDir, "styles.css"));
  fs.copyFileSync(path.join(STATIC_DIR, "app.js"), path.join(targetDir, "app.js"));
  copyRecursiveSync(path.join(STATIC_DIR, "vendor"), path.join(targetDir, "vendor"));

  // 4. Static host routing files
  fs.writeFileSync(path.join(targetDir, "_redirects"), "/api/sites /api/sites.json 200\n/* /index.html 200\n");
  fs.writeFileSync(path.join(targetDir, ".nojekyll"), "");
  fs.writeFileSync(
    path.join(targetDir, "vercel.json"),
    JSON.stringify({ version: 2, rewrites: [{ source: "/api/sites", destination: "/api/sites.json" }] }, null, 2)
  );
}

function verifyTarget(targetDir) {
  const label = path.basename(targetDir);
  const indexHtml = fs.readFileSync(path.join(targetDir, "index.html"), "utf8");
  const regex = /(?:href|src)="([^"]+)"/g;
  let match;
  console.log(`\n=== [${label}/] Asset Path Verification ===`);
  while ((match = regex.exec(indexHtml)) !== null) {
    const ref = match[1];
    if (ref.startsWith("http") || ref.startsWith("data:") || ref.startsWith("#")) continue;
    const cleanRef = ref.replace(/^\.?\//, "");
    const fullPath = path.join(targetDir, cleanRef);
    if (!fs.existsSync(fullPath)) {
      throw new Error(`[${label}] Missing referenced asset: ${ref}`);
    }
    const sizeKb = (fs.statSync(fullPath).size / 1024).toFixed(1);
    console.log(`  [OK] ${ref.padEnd(34)} -> ${cleanRef} (${sizeKb} KB)`);
  }
}

buildTarget(DIST_DIR);
buildTarget(BUILD_DIR);
verifyTarget(DIST_DIR);
verifyTarget(BUILD_DIR);
console.log("\n[SUCCESS] Production build completed in `dist/` and `build/`!");
