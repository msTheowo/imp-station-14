#!/usr/bin/env python

import argparse
import json
import re
from datetime import datetime
from pathlib import Path
from subprocess import run

import yaml

# Backfill a changelog from merged PR :cl: blocks.
# Requires the GitHub CLI to be installed and authenticated.

DEFAULT_CHANGELOG_PATH = Path("Resources/Changelog/QuantumBlue.yml")
TIME_FORMAT = "%Y-%m-%dT%H:%M:%S.0000000%z"
COMMENT_REGEX = re.compile(r"<!--.*?-->", re.DOTALL)
HEADER_REGEX = re.compile(r"^\s*(?::cl:|🆑)\s*([a-z0-9_\- ]+)?\s*$", re.IGNORECASE | re.MULTILINE)
ENTRY_REGEX = re.compile(r"^ *[*-]? *(add|remove|tweak|fix): *([^\n\r]+)\r?$", re.IGNORECASE | re.MULTILINE)


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(description="Backfill a changelog from merged GitHub PRs.")
    parser.add_argument("--repo", default=None, help="GitHub repository in owner/name format. Defaults to the current gh repo.")
    parser.add_argument("--limit", type=int, default=500, help="Maximum number of merged PRs to inspect.")
    parser.add_argument("--output", type=Path, default=DEFAULT_CHANGELOG_PATH, help="Changelog file to append entries to.")
    parser.add_argument("--dry-run", action="store_true", help="Print what would be added without writing the changelog file.")
    parser.add_argument("--overwrite", action="store_true", help="Replace the changelog entries instead of appending missing ones.")
    parser.add_argument("--fallback-to-title", action="store_true", help="Use PR titles for entries when no changelog block is present.")
    parser.add_argument("--fallback-type", choices=("Add", "Remove", "Tweak", "Fix"), default="Tweak", help="Change type to use with --fallback-to-title.")
    return parser.parse_args()


def main() -> None:
    args = parse_args()
    prs = load_pull_requests(args.repo, args.limit)
    existing = load_changelog(args.output)
    existing_urls = set() if args.overwrite else {entry.get("url") for entry in existing["Entries"]}
    next_id = 1 if args.overwrite else get_highest_entry_id(existing["Entries"]) + 1

    new_entries = []
    for pull in sorted(prs, key=lambda pr: pr["mergedAt"]):
        entry = pull_request_to_entry(pull, args.fallback_to_title, args.fallback_type)
        if entry is None:
            continue

        if entry["url"] in existing_urls:
            continue

        entry["id"] = next_id
        next_id += 1
        existing_urls.add(entry["url"])
        new_entries.append(entry)

    if args.dry_run:
        print(yaml.safe_dump({"Entries": new_entries}, sort_keys=False, allow_unicode=False))
        print(f"Dry run complete. Parsed {len(new_entries)} new changelog entries.")
        return

    if args.overwrite:
        existing["Entries"] = new_entries
    else:
        existing["Entries"].extend(new_entries)

    existing["Entries"] = sort_entries_descending(existing["Entries"])

    args.output.write_text(
        yaml.safe_dump(existing, sort_keys=False, allow_unicode=False, width=120),
        encoding="utf-8",
    )
    print(f"Wrote {len(new_entries)} changelog entries to {args.output}")


def load_pull_requests(repo: str | None, limit: int) -> list[dict]:
    command = [
        "gh",
        "pr",
        "list",
        "--state",
        "merged",
        "--limit",
        str(limit),
        "--json",
        "author,body,mergedAt,title,url",
    ]

    if repo:
        command.extend(["--repo", repo])

    process = run(command, capture_output=True, check=False)
    if process.returncode != 0:
        stderr = process.stderr.decode("utf-8", errors="replace").strip()
        raise RuntimeError(stderr or "Failed to load pull requests with gh.")

    return json.loads(process.stdout.decode("utf-8"))


def load_changelog(path: Path) -> dict:
    if not path.exists():
        return {"Entries": []}

    changelog = yaml.safe_load(path.read_text(encoding="utf-8")) or {}
    entries = changelog.get("Entries")
    if not isinstance(entries, list):
        changelog["Entries"] = []

    return changelog


def get_highest_entry_id(entries: list[dict]) -> int:
    return max((int(entry.get("id", 0)) for entry in entries), default=0)


def sort_entries_descending(entries: list[dict]) -> list[dict]:
    return sorted(entries, key=lambda entry: entry.get("time", ""), reverse=True)


def pull_request_to_entry(pull: dict, fallback_to_title: bool = False, fallback_type: str = "Tweak") -> dict | None:
    body = COMMENT_REGEX.sub("", pull.get("body") or "")
    header_match = HEADER_REGEX.search(body)
    changes = get_changes(body)

    if not changes and not fallback_to_title:
        return None

    merged_at = normalize_merged_at(pull["mergedAt"])
    author = pull["author"]["login"]
    if header_match is not None:
        author = (header_match.group(1) or "").strip() or author

    if not changes:
        changes = [{
            "type": fallback_type,
            "message": pull.get("title", "").strip(),
        }]

    return {
        "author": author,
        "changes": changes,
        "time": merged_at,
        "url": pull["url"],
    }


def get_changes(body: str) -> list[dict]:
    changes = []
    for change_type, message in ENTRY_REGEX.findall(body):
        changes.append(
            {
                "type": normalize_change_type(change_type),
                "message": message.strip(),
            }
        )

    return changes


def normalize_change_type(change_type: str) -> str:
    normalized = change_type.lower()
    if normalized == "add":
        return "Add"
    if normalized == "remove":
        return "Remove"
    if normalized == "tweak":
        return "Tweak"
    if normalized == "fix":
        return "Fix"

    raise ValueError(f"Unsupported changelog type: {change_type}")


def normalize_merged_at(merged_at: str) -> str:
    parsed = datetime.fromisoformat(merged_at.replace("Z", "+00:00"))
    formatted = parsed.strftime(TIME_FORMAT)
    return f"{formatted[:-5]}{formatted[-5:-2]}:{formatted[-2:]}"


if __name__ == "__main__":
    main()