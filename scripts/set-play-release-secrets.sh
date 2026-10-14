#!/usr/bin/env bash
# android-release.yml'in ihtiyaç duyduğu GitHub secret'larını ayarlar.
# Değerler ekrana basılmaz: keystore ve şifreler upload key env dosyasından
# (bkz. docs/PLAY-STORE-SUBMISSION.md), JSON anahtarı dosyadan doğrudan
# gh'ye aktarılır.
#
# Kullanım: scripts/set-play-release-secrets.sh <service-account.json> [keystore.env]
set -euo pipefail

if [[ $# -lt 1 || $# -gt 2 ]]; then
  echo "Kullanım: $0 <service-account.json> [keystore.env]" >&2
  exit 1
fi

json_file=$1
env_file=${2:-$HOME/.paydefteri-upload-keystore.env}
repo=HasanErenAkgoz/paydefteri

for f in "$json_file" "$env_file"; do
  [[ -f $f ]] || { echo "Dosya bulunamadı: $f" >&2; exit 1; }
done

set -a
. "$env_file"
set +a
[[ -f $PAYDEFTERI_UPLOAD_STORE_FILE ]] || { echo "Keystore bulunamadı: $PAYDEFTERI_UPLOAD_STORE_FILE" >&2; exit 1; }

gh secret set PLAY_SERVICE_ACCOUNT_JSON --repo "$repo" < "$json_file"
base64 -i "$PAYDEFTERI_UPLOAD_STORE_FILE" | gh secret set PAYDEFTERI_UPLOAD_KEYSTORE_BASE64 --repo "$repo"

for name in PAYDEFTERI_UPLOAD_STORE_PASSWORD PAYDEFTERI_UPLOAD_KEY_ALIAS PAYDEFTERI_UPLOAD_KEY_PASSWORD; do
  printf '%s' "${!name}" | gh secret set "$name" --repo "$repo"
done

echo
gh secret list --repo "$repo"
echo
echo "Tamam. Service account JSON dosyasını artık silebilirsiniz: rm \"$json_file\""
