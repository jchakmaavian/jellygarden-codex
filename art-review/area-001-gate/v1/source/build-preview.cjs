// npm install --prefix /tmp/king-leo-area1-tools three@0.180.0 esbuild@0.25.9
const path = require('node:path');
const dependencies = process.env.KING_LEO_PREVIEW_NODE_MODULES || '/tmp/king-leo-area1-tools/node_modules';
require(path.join(dependencies, 'esbuild')).buildSync({
  entryPoints: [path.join(__dirname, 'gate-scene.js')],
  bundle: true, minify: true, format: 'iife',
  outfile: path.join(__dirname, 'gate-preview.js'),
  nodePaths: [dependencies], legalComments: 'eof',
});
