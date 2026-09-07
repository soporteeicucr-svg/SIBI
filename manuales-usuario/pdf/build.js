const fs = require('fs');
const path = require('path');
const MarkdownIt = require('markdown-it');
const anchor = require('markdown-it-anchor');
const GithubSlugger = require('github-slugger').default || require('github-slugger');

// Raíz del repo: dos niveles arriba de manuales-usuario/pdf/
const REPO = path.resolve(__dirname, '..', '..').replace(/\\/g, '/');
const OUT = path.join(__dirname, 'html');
fs.mkdirSync(OUT, { recursive: true });

const mermaidJs = fs.readFileSync(path.join(__dirname, 'mermaid.min.js'), 'utf8');

const jobs = [
  { md: 'MANUAL_TECNICO.md',                       base: REPO + '/',                  title: 'Manual Técnico — SIBI',                 mermaid: true },
  { md: 'manuales-usuario/Manual_Administradora.md', base: REPO + '/manuales-usuario/', title: 'Manual de Usuario — Administradora',    mermaid: false },
  { md: 'manuales-usuario/Manual_GTI.md',            base: REPO + '/manuales-usuario/', title: 'Manual de Usuario — GTI',               mermaid: false },
  { md: 'manuales-usuario/Manual_JefaAdministrativa.md', base: REPO + '/manuales-usuario/', title: 'Manual de Usuario — Jefa Administrativa', mermaid: false },
  { md: 'manuales-usuario/Manual_Invitado.md',       base: REPO + '/manuales-usuario/', title: 'Manual de Usuario — Invitado',          mermaid: false },
];

function esc(s) {
  return s.replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;');
}

for (const job of jobs) {
  const slugger = new GithubSlugger();
  const md = new MarkdownIt({
    html: true,
    linkify: true,
    typographer: false,
    highlight(str, lang) {
      if (job.mermaid && lang === 'mermaid') {
        return `<pre class="mermaid">${esc(str.trim())}</pre>`;
      }
      return ''; // default <pre><code> escaping
    },
  });
  md.use(anchor, {
    slugify: (s) => slugger.slug(s),
    permalink: false,
    tabIndex: false,
  });

  const src = fs.readFileSync(path.join(REPO, job.md), 'utf8');
  const body = md.render(src);

  const mermaidBlock = job.mermaid
    ? `<script>${mermaidJs}</script>
       <script>
         mermaid.initialize({ startOnLoad: false, theme: 'neutral', securityLevel: 'loose',
           flowchart:{useMaxWidth:true}, er:{useMaxWidth:true}, sequence:{useMaxWidth:true} });
         window.__mre\u0061dy = mermaid.run({ querySelector: '.mermaid' })
           .then(() => { document.body.setAttribute('data-mermaid-done','1'); })
           .catch(e => { console.error(e); document.body.setAttribute('data-mermaid-done','err'); });
       </script>`
    : '';

  const html = `<!doctype html>
<html lang="es">
<head>
<meta charset="utf-8">
<base href="file:///${job.base}">
<title>${job.title}</title>
<style>
  @page { size: A4; margin: 18mm 16mm 20mm 16mm; }
  html { -webkit-print-color-adjust: exact; print-color-adjust: exact; }
  body { font: 11pt/1.5 -apple-system, "Segoe UI", Roboto, Helvetica, Arial, sans-serif; color: #1f2430; max-width: 100%; }
  h1,h2,h3,h4,h5 { color: #003d7a; line-height: 1.25; margin: 1.4em 0 .5em; page-break-after: avoid; }
  h1 { font-size: 20pt; border-bottom: 2px solid #003d7a; padding-bottom: .2em; }
  h2 { font-size: 15pt; border-bottom: 1px solid #cbd5e1; padding-bottom: .15em; }
  h3 { font-size: 12.5pt; }
  h4 { font-size: 11pt; }
  p, li { orphans: 2; widows: 2; }
  a { color: #0066cc; text-decoration: none; }
  code { font-family: "SFMono-Regular", Consolas, "Liberation Mono", monospace; font-size: .88em;
         background: #f1f5f9; padding: .1em .35em; border-radius: 3px; }
  pre { background: #f6f8fa; border: 1px solid #e2e8f0; border-radius: 6px; padding: 10px 12px;
        overflow-x: auto; font-size: .82em; line-height: 1.45; page-break-inside: avoid; }
  pre code { background: none; padding: 0; font-size: 1em; }
  pre.mermaid { background: #fff; border: none; text-align: center; }
  pre.mermaid svg { max-width: 100%; height: auto; }
  table { border-collapse: collapse; width: 100%; margin: 1em 0; font-size: .85em; page-break-inside: avoid; }
  th, td { border: 1px solid #cbd5e1; padding: 5px 8px; text-align: left; vertical-align: top; }
  th { background: #eef4fb; color: #003d7a; }
  tr:nth-child(even) td { background: #f8fafc; }
  blockquote { border-left: 3px solid #93c5fd; margin: 1em 0; padding: .3em 0 .3em 1em; color: #475569; background: #f8fafc; }
  img { max-width: 100%; height: auto; display: block; margin: .6em 0; border: 1px solid #e2e8f0; border-radius: 6px;
        page-break-inside: avoid; }
  em { color: #475569; }
  hr { border: none; border-top: 1px solid #e2e8f0; margin: 1.6em 0; }
  ul, ol { padding-left: 1.4em; }
  :target { background: #fff7cc; }
</style>
</head>
<body>
${body}
${mermaidBlock}
</body>
</html>`;

  const outName = path.basename(job.md).replace(/\.md$/, '.html');
  fs.writeFileSync(path.join(OUT, outName), html);
  console.log('wrote', outName, '(' + (html.length / 1024).toFixed(0) + ' KB)');
}
console.log('done -> ' + OUT);
