#!/usr/bin/env node
// PreToolUse guard for Bash: fails open (exit 0) on any error or uncertainty.
// Only blocks categorical, never-legitimate-in-this-repo patterns (filesystem
// wipe, fork bombs) — never business-judgment calls like force-push or
// `git reset --hard`, which the assistant already gates via user confirmation.

let input = '';
process.stdin.on('data', (chunk) => { input += chunk; });
process.stdin.on('end', () => {
  try {
    const payload = JSON.parse(input || '{}');
    const command = String(payload?.tool_input?.command || '');

    const DENYLIST = [
      /rm\s+(-\w*[rf]\w*){2}\s+(\/|~)(\s|$)/i, // rm -rf / or rm -rf ~
      /rm\s+.*--no-preserve-root/i,
      /:\(\)\s*\{\s*:\s*\|\s*:\s*&\s*\}\s*;\s*:/, // classic fork bomb
      /mkfs\.\w+\s+\/dev\//i,
      /dd\s+.*of=\/dev\/(disk|sd|nvme)/i,
    ];

    const hit = DENYLIST.find((pattern) => pattern.test(command));
    if (hit) {
      console.error(
        `guard-destructive-bash: blocked a categorically destructive command (matched ${hit}). ` +
        'If this is genuinely intended, run it outside Claude Code.'
      );
      process.exit(2);
    }

    process.exit(0);
  } catch (_error) {
    // Never block on a hook bug — fail open.
    process.exit(0);
  }
});
