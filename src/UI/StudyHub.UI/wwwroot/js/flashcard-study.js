// Space is the "Show answer" shortcut on the flashcard study page. Without this it would also scroll
// the page. Only the focused study area itself is affected, so buttons keep their own Space behavior.
document.addEventListener('keydown', function (event) {
    if (event.key === ' ' && event.target instanceof Element && event.target.classList.contains('study-page')) {
        event.preventDefault();
    }
});
