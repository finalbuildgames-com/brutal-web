// Extends modules/webgl2/tests/browser-smoke.mjs's small MIME-aware HTTP host.
import http from 'node:http';
import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';
const here = path.dirname(fileURLToPath(import.meta.url));
export function serve(root = path.join(here, 'build/itch'), encoding = 'identity') {
    root = path.resolve(root);
    if (!["identity", "gzip", "br"].includes(encoding)) throw new Error("Unsupported encoding");
    const extension = encoding === "gzip" ? "gz" : encoding;
    const mime = { '.html': 'text/html; charset=utf-8', '.js': 'text/javascript', '.mjs':'text/javascript', '.json': 'application/json', '.wasm': 'application/wasm', '.txt': 'text/plain', '.png': 'image/png' };
    return http.createServer((req, res) => {
        let file;
        try { file = path.resolve(root, '.' + decodeURIComponent(new URL(req.url, 'http://localhost').pathname)); }
        catch { return res.writeHead(400).end(); }
        if (file !== root && !file.startsWith(root + path.sep)) return res.writeHead(403).end();
        if (file === root || fs.existsSync(file) && fs.statSync(file).isDirectory()) file = path.join(file, 'index.html');
        const headers = { 'content-type': mime[path.extname(file)] ?? 'application/octet-stream', 'cache-control': 'no-store', 'vary': 'Accept-Encoding' };
        if(process.env.FBS_ISOLATE==='1'){headers['cross-origin-opener-policy']='same-origin';headers['cross-origin-embedder-policy']='require-corp';headers['cross-origin-resource-policy']='same-origin';}
        if (encoding !== 'identity' && (req.headers['accept-encoding'] ?? '').includes(encoding) && fs.existsSync(file + '.' + extension)) {
            headers['content-encoding'] = encoding;
            file += '.' + extension;
        }
        if (!fs.existsSync(file) || !fs.statSync(file).isFile()) return res.writeHead(404).end();
        headers['content-length'] = fs.statSync(file).size;
        res.writeHead(200, headers);
        fs.createReadStream(file).pipe(res);
    });
}
if (process.argv[1] && path.resolve(process.argv[1]) === fileURLToPath(import.meta.url)) {
    const server = serve(process.argv[2], process.env.FBS_HTTP_ENCODING ?? 'identity');
    server.listen(Number(process.env.PORT ?? 8127), '127.0.0.1', () => console.log(`http://127.0.0.1:${server.address().port}`));
}
