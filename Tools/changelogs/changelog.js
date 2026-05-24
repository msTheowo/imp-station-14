const fs = require("node:fs");
const path = require("node:path");

const axios = require("axios");
const yaml = require("js-yaml");

const repository = process.env.GITHUB_REPOSITORY;
const prNumber = process.env.PR_NUMBER;
const changelogFile = process.env.CHANGELOG_FILE || "Resources/Changelog/Changelog.yml";

if (process.env.GITHUB_TOKEN) {
    axios.defaults.headers.common.Authorization = `Bearer ${process.env.GITHUB_TOKEN}`;
}

const headerRegex = /^\s*(?::cl:|🆑)\s*([a-z0-9_\- ]+)?\s*$/im;
const entryRegex = /^ *[*-]? *(add|remove|tweak|fix): *([^\n\r]+)\r?$/gim;
const commentRegex = /<!--.*?-->/gs;

async function main() {
    if (!repository || !prNumber) {
        throw new Error("GITHUB_REPOSITORY and PR_NUMBER must be set.");
    }

    const pullRequest = await getPullRequest(repository, prNumber);
    const body = (pullRequest.body || "").replace(commentRegex, "");
    const headerMatch = headerRegex.exec(body);

    if (!headerMatch) {
        console.log(`No changelog block found in PR #${prNumber}, skipping`);
        return;
    }

    const changes = getChanges(body);
    if (changes.length === 0) {
        console.log(`No changelog entries found in PR #${prNumber}, skipping`);
        return;
    }

    if (!pullRequest.merged_at) {
        console.log(`PR #${prNumber} is not merged, skipping`);
        return;
    }

    const changelogPath = path.resolve(__dirname, "..", "..", changelogFile);
    const author = (headerMatch[1] || "").trim() || pullRequest.user.login;
    const data = loadChangelog(changelogPath);
    const entry = {
        author,
        changes,
        id: getHighestEntryId(data.Entries) + 1,
        time: normalizeMergedAt(pullRequest.merged_at),
        url: pullRequest.html_url,
    };

    data.Entries.push(entry);
    data.Entries.sort((a, b) => compareDescending(a.time, b.time));
    fs.writeFileSync(changelogPath, yaml.dump(data, { lineWidth: 120, noRefs: true }));

    console.log(`Changelog updated from PR #${prNumber}`);
}

async function getPullRequest(githubRepository, number) {
    const response = await axios.get(`https://api.github.com/repos/${githubRepository}/pulls/${number}`);
    return response.data;
}

function getChanges(body) {
    const entries = [];

    for (const match of body.matchAll(entryRegex)) {
        const type = normalizeChangeType(match[1]);
        if (!type) {
            continue;
        }

        entries.push({
            type,
            message: match[2].trim(),
        });
    }

    return entries;
}

function normalizeChangeType(type) {
    switch (type.toLowerCase()) {
        case "add":
            return "Add";
        case "remove":
            return "Remove";
        case "tweak":
            return "Tweak";
        case "fix":
            return "Fix";
        default:
            return null;
    }
}

function normalizeMergedAt(mergedAt) {
    return mergedAt.replace(/Z$/i, ".0000000+00:00");
}

function loadChangelog(changelogPath) {
    if (!fs.existsSync(changelogPath)) {
        return { Entries: [] };
    }

    const file = fs.readFileSync(changelogPath, "utf8");
    const data = yaml.load(file) || {};
    if (!Array.isArray(data.Entries)) {
        data.Entries = [];
    }

    return data;
}

function getHighestEntryId(entries) {
    return entries.reduce((highest, entry) => Math.max(highest, Number(entry.id) || 0), 0);
}

function compareDescending(left, right) {
    return right.localeCompare(left);
}

main().catch((error) => {
    console.error(error);
    process.exitCode = 1;
});