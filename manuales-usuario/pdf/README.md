# Manuales en PDF

PDFs generados a partir de los `.md` de este repo:

| PDF | Fuente |
|---|---|
| `MANUAL_TECNICO.pdf` | `../../MANUAL_TECNICO.md` |
| `Manual_Administradora.pdf` | `../Manual_Administradora.md` |
| `Manual_GTI.pdf` | `../Manual_GTI.md` |
| `Manual_JefaAdministrativa.pdf` | `../Manual_JefaAdministrativa.md` |
| `Manual_Invitado.pdf` | `../Manual_Invitado.md` |

**Son artefactos generados** — la fuente de verdad son los `.md`. Regenerarlos tras cada cambio.

El `MANUAL_TECNICO.pdf` conserva la **tabla de contenido y las referencias cruzadas como enlaces internos clicables**: los slugs de los encabezados se generan con `github-slugger`, así que coinciden con los `[texto](#ancla)` del Markdown (mismo algoritmo que GitHub). Los diagramas `mermaid` se rasterizan a SVG vectorial durante el render.

## Regenerar

Requiere **Node** y **Google Chrome** instalados (sin dependencias en el repo).

```bash
cd manuales-usuario/pdf

# 1) dependencias de render (en esta misma carpeta, no en el repo)
npm init -y
npm install markdown-it markdown-it-anchor github-slugger
curl -sL https://cdn.jsdelivr.net/npm/mermaid@10.9.3/dist/mermaid.min.js -o mermaid.min.js

# 2) Markdown -> HTML (deja los .html en ./html/)
node build.js

# 3) HTML -> PDF con Chrome headless (una llamada por manual)
CHROME="/c/Program Files/Google/Chrome/Application/chrome.exe"   # ajustar ruta si aplica
for f in MANUAL_TECNICO Manual_Administradora Manual_GTI Manual_JefaAdministrativa Manual_Invitado; do
  budget=8000; [ "$f" = "MANUAL_TECNICO" ] && budget=40000   # mermaid necesita más tiempo
  "$CHROME" --headless=new --disable-gpu --no-pdf-header-footer \
    --allow-file-access-from-files --run-all-compositor-stages-before-draw \
    --virtual-time-budget=$budget \
    --print-to-pdf="$(pwd)/$f.pdf" "file://$(pwd)/html/$f.html"
done
```

`node_modules/`, `html/`, `package*.json` y `mermaid.min.js` de esta carpeta son temporales de build; no hace falta versionarlos.
