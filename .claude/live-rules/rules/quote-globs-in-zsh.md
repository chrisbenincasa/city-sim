---
description: Quote glob arguments; the shell is zsh
priority: 45
---
- The shell is zsh, not bash. It expands an unquoted glob in an argument before the command sees it,
  and fails the whole line with `no matches found` when nothing in the working directory matches.
- Quote the pattern: `--include='*.cs'`, `--exclude-dir='obj'`, `find . -name '*.toml'`.
- This bites hardest on `grep -rn --include=`, which is the most-used command in this project.
