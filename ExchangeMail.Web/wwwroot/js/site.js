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

window.loadOutlookBriefing = function () {
    // Reset Skeletons
    const summarySkeleton = `
        <div class="skeleton-text mb-2" style="width: 100%;"></div>
        <div class="skeleton-text mb-2" style="width: 90%;"></div>
        <div class="skeleton-text mb-2" style="width: 95%;"></div>
        <div class="skeleton-text" style="width: 80%;"></div>
     `;

    const eventSkeleton = `
        <div class="d-flex mb-3">
            <div class="skeleton-box me-2" style="width: 40px; height: 40px; border-radius: 8px;"></div>
            <div class="flex-grow-1">
                <div class="skeleton-text mb-1" style="width: 80%;"></div>
                <div class="skeleton-text" style="width: 50%;"></div>
            </div>
        </div>
        <div class="d-flex mb-3">
            <div class="skeleton-box me-2" style="width: 40px; height: 40px; border-radius: 8px;"></div>
            <div class="flex-grow-1">
                <div class="skeleton-text mb-1" style="width: 70%;"></div>
                <div class="skeleton-text" style="width: 40%;"></div>
            </div>
        </div>
     `;

    const listSkeleton = `
        <div class="mb-2">
            <div class="skeleton-text mb-1" style="width: 90%;"></div>
        </div>
        <div class="mb-2">
            <div class="skeleton-text mb-1" style="width: 85%;"></div>
        </div>
        <div class="mb-2">
            <div class="skeleton-text mb-1" style="width: 60%;"></div>
        </div>
     `;

    const emailSkeleton = `
        <div class="mb-3">
            <div class="skeleton-text mb-1" style="width: 40%;"></div>
            <div class="skeleton-text" style="width: 90%;"></div>
        </div>
        <div class="mb-3">
            <div class="skeleton-text mb-1" style="width: 30%;"></div>
            <div class="skeleton-text" style="width: 80%;"></div>
        </div>
     `;

    // Disable refresh button while loading
    var refreshBtn = document.getElementById('btn-outlook-refresh');
    if (refreshBtn) refreshBtn.disabled = true;

    document.getElementById('outlook-summary-content').innerHTML = summarySkeleton;
    document.getElementById('outlook-events-list').innerHTML = eventSkeleton;
    document.getElementById('outlook-tasks-list').innerHTML = listSkeleton;
    document.getElementById('outlook-emails-list').innerHTML = emailSkeleton;

    fetch('/Outlook/GetSummary')
        .then(response => response.json())
        .then(data => {
            // Update Greeting Header
            if (data.greeting) {
                document.getElementById('outlook-briefing-header').innerText = data.greeting;
            }

            // Populate Summary
            document.getElementById('outlook-summary-content').innerHTML = data.summary;

            // Populate Events
            const eventsContainer = document.getElementById('outlook-events-list');
            if (data.events && data.events.length > 0) {
                eventsContainer.innerHTML = data.events.map(e => `
                    <div class="d-flex mb-3 align-items-center">
                        <div class="me-3 text-center bg-primary-subtle text-primary rounded p-1" style="min-width: 50px;">
                            <small class="fw-bold d-block">${e.time}</small>
                        </div>
                        <div>
                            <div class="fw-bold">${e.subject}</div>
                            <small class="text-muted"><i class="bi bi-geo-alt-fill me-1"></i>${e.location || 'No Location'}</small>
                        </div>
                    </div>
                 `).join('');
            } else {
                eventsContainer.innerHTML = '<p class="text-muted small">No events scheduled today.</p>';
            }

            // Populate Tasks
            const tasksContainer = document.getElementById('outlook-tasks-list');
            if (data.tasks && data.tasks.length > 0) {
                tasksContainer.innerHTML = '<ul class="list-group list-group-flush">' +
                    data.tasks.map(t => `
                        <li class="list-group-item px-0 py-2 border-0 bg-transparent d-flex align-items-center">
                            <i class="bi bi-circle me-2 text-warning"></i>
                            <span class="${t.overdue ? 'text-danger' : ''}">${t.subject}</span>
                            ${t.overdue ? '<span class="badge bg-danger ms-auto">Overdue</span>' : ''}
                        </li>
                    `).join('') +
                    '</ul>';
            } else {
                tasksContainer.innerHTML = '<p class="text-muted small">No pending tasks.</p>';
            }

            // Populate Emails
            const emailsContainer = document.getElementById('outlook-emails-list');
            if (data.emails && data.emails.length > 0) {
                emailsContainer.innerHTML = '<div class="list-group list-group-flush">' +
                    data.emails.map(m => `
                        <div class="list-group-item px-0 py-2 border-0 bg-transparent">
                            <div class="fw-bold text-truncate">${m.from}</div>
                            <div class="small text-truncate text-muted">${m.subject}</div>
                        </div>
                    `).join('') +
                    '</div>';
            } else {
                emailsContainer.innerHTML = '<p class="text-muted small">No important unread emails.</p>';
            }
        })
        .catch(err => {
            console.error(err);
            document.getElementById('outlook-summary-content').innerHTML = '<p class="text-danger">Failed to load briefing.</p>';
        })
        .finally(() => {
            if (refreshBtn) refreshBtn.disabled = false;
        });
};

window.showOutlookModal = function () {
    var modalEl = document.getElementById('outlookModal');
    var modal = new bootstrap.Modal(modalEl);
    modal.show();

    // Attach refresh handler if not already
    var refreshBtn = document.getElementById('btn-outlook-refresh');
    if (refreshBtn) {
        // Use .onclick to replace any existing handler to avoid duplicates
        refreshBtn.onclick = function () {
            window.loadOutlookBriefing();
        };
    }

    // Load initial data
    window.loadOutlookBriefing();
};

// Threading Logic
// Threading Logic
$(document).on('click', '.thread-expander', function (e) {
    e.stopPropagation();
    e.preventDefault();

    const threadId = $(this).data('thread-id');
    const container = document.getElementById(`thread-${threadId}`);
    const icon = document.getElementById(`icon-${threadId}`);

    console.log('Toggling thread:', threadId, container);

    if (!container) return;

    const isExpanded = container.classList.contains('show');

    if (isExpanded) {
        // Collapse
        container.classList.remove('show');
        icon.classList.remove('bi-chevron-down');
        icon.classList.add('bi-chevron-right');
    } else {
        // Expand
        // Check if loaded
        if (container.innerHTML.trim() === '') {
            container.innerHTML = '<div class="text-center p-2"><div class="spinner-border spinner-border-sm text-secondary" role="status"></div></div>';

            fetch(`/Mail/GetThreadMessages?threadId=${threadId}`)
                .then(response => {
                    if (!response.ok) throw new Error('Network response was not ok');
                    return response.text();
                })
                .then(html => {
                    container.innerHTML = html;
                })
                .catch(err => {
                    console.error('Error loading thread:', err);
                    container.innerHTML = '<div class="text-danger small p-2">Error loading messages</div>';
                });
        }

        container.classList.add('show');
        icon.classList.remove('bi-chevron-right');
        icon.classList.add('bi-chevron-down');
    }
});

// Pull to Refresh Logic
document.addEventListener('DOMContentLoaded', () => {
    const container = document.getElementById('messageListContainer');
    const indicator = document.getElementById('ptr-indicator');

    if (!container || !indicator) return;

    let startY = 0;
    let currentY = 0;
    let isDragging = false;
    const threshold = 80;

    container.addEventListener('touchstart', (e) => {
        if (container.scrollTop === 0) {
            startY = e.touches[0].clientY;
            isDragging = true;
        }
    }, { passive: true });

    container.addEventListener('touchmove', (e) => {
        if (!isDragging) return;

        currentY = e.touches[0].clientY;
        const diff = currentY - startY;

        // Only handle pull down when at top
        if (diff > 0 && container.scrollTop === 0) {
            // Visualize stretch (dampened)
            if (diff < threshold * 2) {
                indicator.style.height = `${Math.min(diff / 2, 60)}px`;
                indicator.style.opacity = `${Math.min(diff / threshold, 1)}`;
            }
        } else {
            isDragging = false;
            indicator.style.height = '0px';
            indicator.style.opacity = '0';
        }
    }, { passive: true });

    container.addEventListener('touchend', (e) => {
        if (!isDragging) return;

        const diff = currentY - startY;
        if (diff > threshold && container.scrollTop === 0) {
            // Trigger Refresh
            indicator.style.height = '60px'; // Lock open
            indicator.style.opacity = '1';

            location.reload();
        } else {
            // Reset
            indicator.style.height = '0px';
            indicator.style.opacity = '0';
        }
        isDragging = false;
        startY = 0;
        currentY = 0;
    });
});
