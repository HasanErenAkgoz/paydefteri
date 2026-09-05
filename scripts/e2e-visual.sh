#!/usr/bin/env bash
# Görsel regresyon baseline'larını CI ile aynı Linux ortamında üretir/doğrular.
#
# Neden Docker: Playwright ekran görüntülerini platforma göre adlandırır
# (…-linux.png / …-darwin.png). macOS'ta üretilen baseline CI'da (ubuntu) hiç
# kullanılmaz. Bu script, CI'daki ile aynı Playwright imajında koşarak
# doğrudan "-linux" baseline'ları üretir.
#
# Ön koşul (host tarafında):
#   docker compose up -d
#   dotnet run --project src/api/PayDefteri.Api
#   npm start --prefix src/web -- --host 0.0.0.0   # container'ın erişebilmesi için
#
# Kullanım:
#   scripts/e2e-visual.sh --update-snapshots   # baseline üret/yenile
#   scripts/e2e-visual.sh                      # mevcut baseline'lara karşı doğrula
set -euo pipefail

IMAGE="mcr.microsoft.com/playwright:v1.63.0-noble"
WEB_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/../src/web" && pwd)"

docker run --rm \
  --add-host=host.docker.internal:host-gateway \
  -v "$WEB_DIR":/work \
  -w /work \
  -e PLAYWRIGHT_BROWSERS_PATH=/ms-playwright \
  -e E2E_VISUAL=1 \
  "$IMAGE" \
  bash -c '
    set -e
    node e2e/docker-tcp-proxy.cjs 4200 5096 &
    for i in $(seq 1 30); do
      curl -sf http://localhost:4200/ >/dev/null && break
      sleep 1
    done
    curl -sf http://localhost:4200/ >/dev/null || {
      echo "Angular sunucusuna ulaşılamadı. Host tarafında: npm start -- --host 0.0.0.0" >&2
      exit 1
    }
    # "$@" ile ilet: tırnaklı argümanlar (ör. --grep "giriş ekranı") bozulmasın.
    exec npx playwright test "$@"
  ' _ "$@"
