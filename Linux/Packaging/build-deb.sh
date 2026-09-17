#!/usr/bin/env bash
set -euo pipefail

runtime="${1:-linux-x64}"
version="${2:-0.1.0}"
output_dir="${3:-artifacts/deb}"

case "$runtime" in
  linux-x64) deb_arch="amd64" ;;
  linux-arm64) deb_arch="arm64" ;;
  *) echo "Unsupported runtime: $runtime" >&2; exit 2 ;;
esac

root="$(mktemp -d)"
trap 'rm -rf "$root"' EXIT
package_root="$root/lertaro"
install_root="$package_root/usr/lib/lertaro"

mkdir -p \
  "$package_root/DEBIAN" \
  "$install_root" \
  "$package_root/usr/bin" \
  "$package_root/usr/share/applications" \
  "$package_root/usr/lib/systemd/user" \
  "$output_dir"

for project in Cli Daemon App; do
  source_dir="artifacts/$runtime/$project"
  test -d "$source_dir" || { echo "Missing publish output: $source_dir" >&2; exit 3; }
  cp -a "$source_dir/." "$install_root/"
done

cat > "$package_root/DEBIAN/control" <<EOF
Package: lertaro
Version: $version
Section: utils
Priority: optional
Architecture: $deb_arch
Maintainer: Lertaro Linux contributors
Description: Fast local file search and launcher for Linux
EOF

cat > "$package_root/usr/lib/systemd/user/lertaro.service" <<'EOF'
[Unit]
Description=Lertaro Linux search daemon
After=graphical-session.target

[Service]
Type=simple
ExecStart=/usr/lib/lertaro/lertarod
Restart=on-failure
RestartSec=2

[Install]
WantedBy=default.target
EOF

install -m 0644 Linux/Packaging/lertaro.desktop "$package_root/usr/share/applications/lertaro.desktop"
ln -s ../lib/lertaro/lertaro-linux "$package_root/usr/bin/lertaro"
ln -s ../lib/lertaro/lertaro-ui "$package_root/usr/bin/lertaro-ui"

package="$output_dir/lertaro_${version}_${deb_arch}.deb"
dpkg-deb --root-owner-group --build "$package_root" "$package"
echo "$package"
