// Loading state for every form marked with data-loading: once a valid form is submitted,
// its submit button is disabled and shows a spinner, so a slow API call cannot be sent twice.
(function () {
    function isInvalid(form) {
        const $ = window.jQuery;
        return $ && $(form).data('validator') && !$(form).valid();
    }

    document.addEventListener('submit', function (event) {
        const form = event.target;
        if (!form.hasAttribute('data-loading') || event.defaultPrevented || isInvalid(form)) {
            return;
        }
        form.setAttribute('aria-busy', 'true');
        form.querySelectorAll('button[type="submit"]').forEach(function (button) {
            button.disabled = true;
            if (button === event.submitter || !event.submitter) {
                button.dataset.originalText = button.innerHTML;
                const text = form.getAttribute('data-loading') || 'Working…';
                button.innerHTML = '<span class="spinner-border spinner-border-sm me-2" aria-hidden="true"></span>' + text;
            }
        });
    });

    // Coming back with the browser's Back button restores the page from cache with the
    // buttons still disabled; put them back.
    window.addEventListener('pageshow', function () {
        document.querySelectorAll('form[aria-busy="true"]').forEach(function (form) {
            form.removeAttribute('aria-busy');
            form.querySelectorAll('button[type="submit"]').forEach(function (button) {
                button.disabled = false;
                if (button.dataset.originalText) {
                    button.innerHTML = button.dataset.originalText;
                }
            });
        });
    });
})();
