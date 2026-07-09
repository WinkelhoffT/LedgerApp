window.studyHubTheme = {
    storageKey: 'studyhub-dark-mode',

    getIsDarkMode: function () {
        const stored = localStorage.getItem(this.storageKey);
        if (stored !== null) {
            return stored === 'true';
        }
        return window.matchMedia('(prefers-color-scheme: dark)').matches;
    },

    setIsDarkMode: function (isDarkMode) {
        localStorage.setItem(this.storageKey, isDarkMode ? 'true' : 'false');
    }
};
