// Dark Mode Toggle Logic
document.addEventListener('DOMContentLoaded', () => {
    const darkModeToggle = document.getElementById('darkModeToggle');
    const htmlElement = document.documentElement;

    // Check local storage or default to dark
    const savedTheme = localStorage.getItem('theme') || 'dark';
    htmlElement.setAttribute('data-bs-theme', savedTheme);

    if (darkModeToggle) {
        // Set initial checkbox state
        darkModeToggle.checked = savedTheme === 'dark';

        darkModeToggle.addEventListener('change', () => {
            const newTheme = darkModeToggle.checked ? 'dark' : 'light';
            htmlElement.setAttribute('data-bs-theme', newTheme);
            localStorage.setItem('theme', newTheme);
        });
    }
});

// Sidebar Toggle Logic
$(document).ready(function () {
    $('#sidebarToggle').on('click', function () {
        $('body').toggleClass('show-sidebar');
    });

    // Close sidebar when clicking outside on mobile
    $(document).on('click', function (e) {
        if ($('body').hasClass('show-sidebar') &&
            !$(e.target).closest('.mail-sidebar').length &&
            !$(e.target).closest('#sidebarToggle').length) {
            $('body').removeClass('show-sidebar');
        }
    });
});

// Global Modal Helpers
window.showModalAlert = function (message, title = 'Alert') {
    return new Promise((resolve) => {
        $('#globalAlertTitle').text(title);
        $('#globalAlertMessage').text(message);
        var modal = new bootstrap.Modal(document.getElementById('globalAlertModal'));

        // Resolve when hidden
        $('#globalAlertModal').one('hidden.bs.modal', function () {
            resolve();
        });

        modal.show();
    });
};

window.showModalConfirm = function (message, title = 'Confirm') {
    return new Promise((resolve) => {
        $('#globalConfirmTitle').text(title);
        $('#globalConfirmMessage').text(message);
        var modal = new bootstrap.Modal(document.getElementById('globalConfirmModal'));

        // Clean up previous events
        $('#globalConfirmYes').off('click');
        $('#globalConfirmCancel').off('click');
        $('#globalConfirmClose').off('click');

        var resolved = false;

        $('#globalConfirmYes').on('click', function () {
            resolved = true;
            modal.hide();
            resolve(true);
        });

        $('#globalConfirmCancel, #globalConfirmClose').on('click', function () {
            if (!resolved) {
                resolved = true;
                // modal.hide() is handled by data-bs-dismiss for cancel, but we need to resolve
                resolve(false);
            }
        });

        // Fallback if dismissed by other means (e.g. escape key if enabled, though static backdrop prevents it usually)
        $('#globalConfirmModal').one('hidden.bs.modal', function () {
            if (!resolved) resolve(false);
        });

        modal.show();
    });
};

window.showModalPrompt = function (message, defaultValue = '', title = 'Prompt') {
    return new Promise((resolve) => {
        $('#globalPromptTitle').text(title);
        $('#globalPromptMessage').text(message);
        $('#globalPromptInput').val(defaultValue);
        var modal = new bootstrap.Modal(document.getElementById('globalPromptModal'));

        // Clean up previous events
        $('#globalPromptOK').off('click');
        $('#globalPromptCancel').off('click');
        $('#globalPromptClose').off('click');
        $('#globalPromptInput').off('keypress');

        var resolved = false;

        $('#globalPromptOK').on('click', function () {
            resolved = true;
            var value = $('#globalPromptInput').val();
            modal.hide();
            resolve(value);
        });

        // Allow Enter key to submit
        $('#globalPromptInput').on('keypress', function (e) {
            if (e.which == 13) {
                resolved = true;
                var value = $('#globalPromptInput').val();
                modal.hide();
                resolve(value);
            }
        });

        $('#globalPromptCancel, #globalPromptClose').on('click', function () {
            if (!resolved) {
                resolved = true;
                resolve(null);
            }
        });

        $('#globalPromptModal').one('hidden.bs.modal', function () {
            if (!resolved) resolve(null);
        });

        // Focus input when shown
        $('#globalPromptModal').one('shown.bs.modal', function () {
            $('#globalPromptInput').focus();
        });

        modal.show();
    });
};

// Sidebar Collapse Logic
$(document).ready(function () {
    const sidebarState = localStorage.getItem('sidebar-collapsed');
    if (sidebarState === 'true') {
        $('body').addClass('sidebar-collapsed');
        $('#sidebar-collapse-icon').removeClass('bi-chevron-left').addClass('bi-chevron-right');
    }

    $('#sidebarCollapseToggle').on('click', function () {
        $('body').toggleClass('sidebar-collapsed');
        const isCollapsed = $('body').hasClass('sidebar-collapsed');
        localStorage.setItem('sidebar-collapsed', isCollapsed);

        // Toggle icon
        if (isCollapsed) {
            $('#sidebar-collapse-icon').removeClass('bi-chevron-left').addClass('bi-chevron-right');
        } else {
            $('#sidebar-collapse-icon').removeClass('bi-chevron-right').addClass('bi-chevron-left');
        }
    });
});

// Settings Modal
window.openSettingsModal = function () {
    var modal = new bootstrap.Modal(document.getElementById('settingsModal'));
    modal.show();
};
