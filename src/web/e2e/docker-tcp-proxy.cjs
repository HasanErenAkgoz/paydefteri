#!/usr/bin/env node
/**
 * Görsel baseline'lar CI ile aynı Linux ortamında üretilmeli; bunun için testler
 * Playwright'ın Docker imajında koşuyor. Ancak uygulama API'ye mutlak adresle
 * gidiyor (environment.ts -> http://localhost:5096/api) ve container içinde
 * "localhost" host makine değil container'ın kendisi.
 *
 * Bu yüzden container içinde aynı portları host'a yönlendiriyoruz: böylece hem
 * sayfa hem API çağrıları dışarıdaki gibi localhost üzerinden çözülür ve API'nin
 * CORS ayarı (http://localhost:4200) olduğu gibi geçerli kalır.
 *
 * Kullanım (container içinde): node e2e/docker-tcp-proxy.cjs 4200 5096
 */
const net = require('node:net');

const UPSTREAM_HOST = process.env.PROXY_UPSTREAM_HOST || 'host.docker.internal';
const ports = process.argv.slice(2).map(Number).filter((port) => Number.isInteger(port) && port > 0);

if (ports.length === 0) {
  console.error('En az bir port verin: node e2e/docker-tcp-proxy.cjs 4200 5096');
  process.exit(1);
}

for (const port of ports) {
  net
    .createServer((client) => {
      const upstream = net.connect(port, UPSTREAM_HOST);
      client.on('error', () => upstream.destroy());
      upstream.on('error', () => client.destroy());
      client.pipe(upstream);
      upstream.pipe(client);
    })
    .listen(port, '127.0.0.1', () => {
      console.log(`proxy: 127.0.0.1:${port} -> ${UPSTREAM_HOST}:${port}`);
    });
}
