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

  function updateButtons() {
    const dark = isDarkActive();
    document.querySelectorAll("[data-theme-toggle]").forEach((btn) => {
      btn.setAttribute("aria-pressed", String(dark));
      btn.textContent = dark ? "☀️ Claro" : "🌙 Oscuro";
      btn.title = dark ? "Cambiar a modo claro" : "Cambiar a modo oscuro";
    });
  }

  function applyTheme(mode) {
    if (mode === "light" || mode === "dark") {
      root.setAttribute("data-theme", mode);
    } else {
      root.removeAttribute("data-theme");
    }
    updateButtons();
  }

  applyTheme(localStorage.getItem(STORAGE_KEY));

  document.addEventListener("click", (ev) => {
    const btn = ev.target.closest("[data-theme-toggle]");
    if (!btn) return;
    const next = isDarkActive() ? "light" : "dark";
    localStorage.setItem(STORAGE_KEY, next);
    applyTheme(next);
  });

  if (window.matchMedia) {
    window.matchMedia("(prefers-color-scheme: dark)").addEventListener("change", () => {
      if (!localStorage.getItem(STORAGE_KEY)) updateButtons();
    });
  }

  if ("serviceWorker" in navigator) {
    window.addEventListener("load", () => {
      navigator.serviceWorker.register("/sw.js").catch(() => {});
    });
  }
})();
