// Bridges the persisted color-scheme preference (localStorage) to the `data-theme`
// attribute the CSS tokens in app.css switch on. The inline script in App.razor applies
// the stored/default theme synchronously before first paint; these functions let the
// Blazor circuit read that state back and update it when the user toggles.
window.studyHubTheme = {
  storageKey: 'studyhub-theme',
  defaultTheme: 'dark',

  get: function () {
    return document.documentElement.getAttribute('data-theme') || this.defaultTheme;
  },

  set: function (theme) {
    document.documentElement.setAttribute('data-theme', theme);
    localStorage.setItem(this.storageKey, theme);
  },
};
