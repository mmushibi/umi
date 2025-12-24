const http = require('http');
const fs = require('fs');
const path = require('path');

const PORT = 3000;
const PUBLIC_DIR = path.join(__dirname, 'public');
const PORTALS_DIR = path.join(__dirname, 'portals');

// MIME types
const mimeTypes = {
  '.html': 'text/html',
  '.js': 'text/javascript',
  '.css': 'text/css',
  '.json': 'application/json',
  '.png': 'image/png',
  '.jpg': 'image/jpeg',
  '.gif': 'image/gif',
  '.svg': 'image/svg+xml',
  '.ico': 'image/x-icon'
};

const getMimeType = (filePath) => {
  const ext = path.extname(filePath).toLowerCase();
  return mimeTypes[ext] || 'application/octet-stream';
};

const server = http.createServer((req, res) => {
  let filePath;
  
  // Determine which directory to serve from
  if (req.url.startsWith('/portals/')) {
    filePath = path.join(PORTALS_DIR, req.url.replace('/portals/', ''));
  } else {
    filePath = path.join(PUBLIC_DIR, req.url === '/' ? 'index.html' : req.url);
  }
  
  // Security: prevent directory traversal
  if (filePath.includes('..')) {
    res.writeHead(400);
    res.end('Bad Request');
    return;
  }

  // Remove query parameters from file path
  filePath = filePath.split('?')[0];

  fs.readFile(filePath, (err, content) => {
    if (err) {
      // Try to serve index.html for directories
      if (err.code === 'EISDIR') {
        filePath = path.join(filePath, 'index.html');
        fs.readFile(filePath, (indexErr, indexContent) => {
          if (indexErr) {
            res.writeHead(404);
            res.end('File Not Found');
            return;
          }
          res.writeHead(200, { 'Content-Type': getMimeType(filePath) });
          res.end(indexContent);
        });
        return;
      }
      
      res.writeHead(404);
      res.end('File Not Found');
      return;
    }

    const mimeType = getMimeType(filePath);
    res.writeHead(200, { 
      'Content-Type': mimeType,
      'Access-Control-Allow-Origin': '*',
      'Access-Control-Allow-Methods': 'GET, POST, PUT, DELETE, OPTIONS',
      'Access-Control-Allow-Headers': 'Content-Type, Authorization'
    });
    res.end(content);
  });
});

server.listen(PORT, () => {
  console.log(`Development server running at http://localhost:${PORT}`);
  console.log(`Serving files from: ${PUBLIC_DIR}`);
});
