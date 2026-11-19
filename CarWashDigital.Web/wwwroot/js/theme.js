(() => {
  const STORAGE_KEY = "cwd_theme";
  const root = document.documentElement;

  function prefersDark() {
    return window.matchMedia && window.matchMedia("(prefers-color-scheme: dark)").matches;
  }

  function isDarkActive() {
    const attr = root.getAttribute("data-theme");
    if (attr === "dark") return true;
    if (attr === "light") return false;
    return prefersDark();
  }

  function updateSwitches() {
    const dark = isDarkActive();
    root.setAttribute("data-bs-theme", dark ? "dark" : "light");
    document.querySelectorAll("[data-theme-toggle]").forEach((el) => {
      el.setAttribute("aria-pressed", String(dark));
      el.setAttribute("aria-label", dark ? "Cambiar a modo claro" : "Cambiar a modo oscuro");
      el.setAttribute("title", dark ? "Modo claro" : "Modo oscuro");
      if (el.tagName === "INPUT" && el.type === "checkbox") {
        el.checked = dark;
      }
    });
  }

  function applyTheme(mode) {
    if (mode === "light" || mode === "dark") {
      root.setAttribute("data-theme", mode);
    } else {
      root.removeAttribute("data-theme");
    }
    updateSwitches();
  }

  applyTheme(localStorage.getItem(STORAGE_KEY));

  document.addEventListener("change", (ev) => {
    const sw = ev.target.closest("[data-theme-toggle]");
    if (!sw) return;
    const next = sw.checked ? "dark" : "light";
    localStorage.setItem(STORAGE_KEY, next);
    applyTheme(next);
  });

  document.addEventListener("click", (ev) => {
    const btn = ev.target.closest("button[data-theme-toggle]");
    if (!btn) return;
    const next = isDarkActive() ? "light" : "dark";
    localStorage.setItem(STORAGE_KEY, next);
    applyTheme(next);
  });

  if (window.matchMedia) {
    window.matchMedia("(prefers-color-scheme: dark)").addEventListener("change", () => {
      if (!localStorage.getItem(STORAGE_KEY)) updateSwitches();
    });
  }

  if ("serviceWorker" in navigator) {
    window.addEventListener("load", () => {
      navigator.serviceWorker.register("/sw.js").catch(() => {});
    });
  }
})();
