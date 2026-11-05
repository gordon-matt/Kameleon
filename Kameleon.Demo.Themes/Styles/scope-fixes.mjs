// Rewrites selectors that sass nested under .bs-theme but that only make sense at the document root, so the
// scope itself carries the variables and the base text styles. Also lets Bootstrap's dark mode follow the
// host's data-theme="dark" attribute on <html>, which is a common convention, instead of needing data-bs-theme.
import { readFileSync, writeFileSync } from "node:fs";

const file = process.argv[2];
let css = readFileSync(file, "utf8");

// Only the rule that declares the dark variables is widened. Longer selectors such as
// ".bs-theme [data-bs-theme=dark] .navbar-toggler-icon" must keep meaning "inside a dark element".
css = css
    .replaceAll(".bs-theme :root,.bs-theme [data-bs-theme=light]", ".bs-theme")
    .replace(/\.bs-theme \[data-bs-theme=dark\]\{/g, ".bs-theme[data-bs-theme=dark],[data-theme=dark] .bs-theme,.bs-theme [data-bs-theme=dark]{")
    .replaceAll(".bs-theme :root", ".bs-theme")
    .replaceAll(".bs-theme html", ".bs-theme")
    .replaceAll(".bs-theme body", ".bs-theme");

writeFileSync(file, css);
console.log(`Scoped ${file} (${css.length} bytes)`);
