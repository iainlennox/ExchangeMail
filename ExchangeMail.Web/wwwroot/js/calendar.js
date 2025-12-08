const calendar = {
    currentDate: new Date(),
    view: 'month', // month, week, day
    events: [],

    init: function () {
        this.render();
        this.loadEvents();
    },

    loadEvents: function () {
        const start = this.getViewStart();
        const end = this.getViewEnd();

        // Format for API
        const startStr = start.toISOString();
        const endStr = end.toISOString();

        fetch(`/Calendar/GetEvents?start=${startStr}&end=${endStr}`)
            .then(res => res.json())
            .then(data => {
                this.events = data.map(e => ({ ...e, start: new Date(e.start), end: new Date(e.end) }));
                this.render(); // Re-render with events
            })
            .catch(err => console.error("Error loading events", err));
    },

    getViewStart: function () {
        const d = new Date(this.currentDate);
        if (this.view === 'month') {
            d.setDate(1);
            // Go back to start of week (Sunday)
            const day = d.getDay();
            d.setDate(d.getDate() - day);
        } else if (this.view === 'week') {
            const day = d.getDay();
            d.setDate(d.getDate() - day);
        }
        d.setHours(0, 0, 0, 0);
        return d;
    },

    getViewEnd: function () {
        const d = this.getViewStart();
        if (this.view === 'month') {
            // Add 6 weeks to cover all possible month displays
            d.setDate(d.getDate() + 42);
        } else if (this.view === 'week') {
            d.setDate(d.getDate() + 7);
        } else {
            d.setDate(d.getDate() + 1);
        }
        return d;
    },

    render: function () {
        this.updateHeader();
        const container = document.getElementById('calendarGrid');
        container.innerHTML = '';

        if (this.view === 'month') {
            this.renderMonth(container);
        } else if (this.view === 'week') {
            this.renderWeek(container);
        } else if (this.view === 'day') {
            this.renderDay(container);
        }

        // Update active buttons
        document.querySelectorAll('.btn-group .btn').forEach(b => b.classList.remove('active', 'btn-primary'));
        document.querySelectorAll('.btn-group .btn').forEach(b => b.classList.add('btn-outline-primary'));

        const activeBtn = document.getElementById(`view${this.view.charAt(0).toUpperCase() + this.view.slice(1)}`);
        if (activeBtn) {
            activeBtn.classList.remove('btn-outline-primary');
            activeBtn.classList.add('active', 'btn-primary');
        }
    },

    updateHeader: function () {
        const options = { month: 'long', year: 'numeric' };
        document.getElementById('currentMonthYear').textContent = this.currentDate.toLocaleDateString(undefined, options);
    },

    renderMonth: function (container) {
        // Header
        const headerRow = document.createElement('div');
        headerRow.className = 'calendar-header-row';
        ['Sun', 'Mon', 'Tue', 'Wed', 'Thu', 'Fri', 'Sat'].forEach(day => {
            const cell = document.createElement('div');
            cell.className = 'calendar-header-cell';
            cell.textContent = day;
            headerRow.appendChild(cell);
        });
        container.appendChild(headerRow);

        // Grid
        const grid = document.createElement('div');
        grid.className = 'calendar-month-grid';

        const start = this.getViewStart();
        const currentMonth = this.currentDate.getMonth();
        const todayStr = new Date().toDateString();

        for (let i = 0; i < 42; i++) { // 6 weeks * 7 days
            const date = new Date(start);
            date.setDate(start.getDate() + i);

            const cell = document.createElement('div');
            cell.className = 'calendar-day-cell';
            const dateStr = date.toDateString();

            if (date.getMonth() !== currentMonth) cell.classList.add('other-month');
            if (dateStr === todayStr) cell.classList.add('today');

            cell.onclick = (e) => this.onDayClick(date, e);

            const number = document.createElement('span');
            number.className = 'day-number';
            number.textContent = date.getDate();
            cell.appendChild(number);

            // Events
            const dayEvents = this.events.filter(e => {
                const eStart = new Date(e.start).toDateString();
                return eStart === dateStr;
            });

            dayEvents.forEach(e => {
                const el = document.createElement('div');
                el.className = 'calendar-event';
                el.textContent = e.title;
                if (e.end < new Date()) el.classList.add('past-event');
                el.onclick = (ev) => {
                    ev.stopPropagation();
                    this.onEventClick(e);
                };
                cell.appendChild(el);
            });

            grid.appendChild(cell);
        }
        container.appendChild(grid);
    },

    renderWeek: function (container) {
        this.renderTimeGrid(container, 7);
    },

    renderDay: function (container) {
        this.renderTimeGrid(container, 1);
    },

    renderTimeGrid: function (container, days) {
        const gridContainer = document.createElement('div');
        gridContainer.className = 'time-grid-container';

        // Time Axis
        const timeAxis = document.createElement('div');
        timeAxis.className = 'time-axis';
        for (let i = 0; i < 24; i++) {
            const slot = document.createElement('div');
            slot.className = 'time-slot-label';
            slot.textContent = `${i}:00`;
            timeAxis.appendChild(slot);
        }
        gridContainer.appendChild(timeAxis);

        // Day Columns
        const dayContainer = document.createElement('div');
        dayContainer.className = 'day-columns-container';
        dayContainer.style.gridTemplateColumns = `repeat(${days}, 1fr)`;

        const start = this.getViewStart();
        if (days === 1) start.setDate(this.currentDate.getDate()); // For day view, use current date exactly

        for (let i = 0; i < days; i++) {
            const date = new Date(start);
            date.setDate(start.getDate() + i);

            const col = document.createElement('div');
            col.className = 'day-column';

            // Header for column
            const colHeader = document.createElement('div');
            colHeader.className = 'text-center p-2 border-bottom fw-bold';
            colHeader.textContent = date.toLocaleDateString(undefined, { weekday: 'short', day: 'numeric' });
            col.appendChild(colHeader);

            // Click to add
            col.onclick = (e) => {
                if (e.target === col || e.target.classList.contains('time-slot-row')) {
                    // Calculate time
                    const rect = col.getBoundingClientRect();
                    const y = e.clientY - rect.top - 40; // Offset header roughly
                    // Approximate hour
                    const hour = Math.floor(y / 60); // 60px per hour
                    date.setHours(hour, 0, 0, 0);
                    this.onDayClick(date, e);
                }
            }

            // Time Slots background
            for (let h = 0; h < 24; h++) {
                const slot = document.createElement('div');
                slot.className = 'time-slot-row';
                col.appendChild(slot);
            }

            // Events
            const dateStr = date.toDateString();
            const dayEvents = this.events.filter(e => new Date(e.start).toDateString() === dateStr);

            dayEvents.forEach(e => {
                const startTime = new Date(e.start);
                const endTime = new Date(e.end);

                const startHour = startTime.getHours() + (startTime.getMinutes() / 60);
                const endHour = endTime.getHours() + (endTime.getMinutes() / 60);
                const duration = endHour - startHour;

                const el = document.createElement('div');
                el.className = 'time-event';
                el.textContent = e.title;

                // Top: Header(40px) + (StartHour * 60px)
                el.style.top = `${40 + (startHour * 60)}px`;
                el.style.height = `${Math.max(duration * 60, 25)}px`; // Min 25px

                el.onclick = (ev) => {
                    ev.stopPropagation();
                    this.onEventClick(e);
                };
                col.appendChild(el);
            });

            dayContainer.appendChild(col);
        }
        gridContainer.appendChild(dayContainer);
        container.appendChild(gridContainer);
    },

    prev: function () {
        if (this.view === 'month') {
            this.currentDate.setMonth(this.currentDate.getMonth() - 1);
        } else if (this.view === 'week') {
            this.currentDate.setDate(this.currentDate.getDate() - 7);
        } else {
            this.currentDate.setDate(this.currentDate.getDate() - 1);
        }
        this.init(); // Reload
    },

    next: function () {
        if (this.view === 'month') {
            this.currentDate.setMonth(this.currentDate.getMonth() + 1);
        } else if (this.view === 'week') {
            this.currentDate.setDate(this.currentDate.getDate() + 7);
        } else {
            this.currentDate.setDate(this.currentDate.getDate() + 1);
        }
        this.init();
    },

    today: function () {
        this.currentDate = new Date();
        this.init();
    },

    changeView: function (v) {
        this.view = v;
        this.init();
    },

    // Modal Interaction
    openAddEventModal: function (date = new Date()) {
        document.getElementById('eventId').value = 0;
        document.getElementById('eventSubject').value = '';
        document.getElementById('eventLocation').value = '';
        document.getElementById('eventDescription').value = '';
        document.getElementById('eventAllDay').checked = false;
        document.getElementById('btnDeleteEvent').style.display = 'none';
        document.getElementById('eventModalTitle').textContent = 'Add Event';

        // Set default times (nearest hour)
        const start = new Date(date);
        start.setMinutes(0, 0, 0);
        const end = new Date(start);
        end.setHours(start.getHours() + 1);

        document.getElementById('eventStart').value = this.toLocalISOString(start);
        document.getElementById('eventEnd').value = this.toLocalISOString(end);

        const modal = new bootstrap.Modal(document.getElementById('eventModal'));
        modal.show();
    },

    onDayClick: function (date, e) {
        this.openAddEventModal(date);
    },

    onEventClick: function (event) {
        document.getElementById('eventId').value = event.id;
        document.getElementById('eventSubject').value = event.title;
        document.getElementById('eventLocation').value = event.location || '';
        document.getElementById('eventDescription').value = event.description || '';
        document.getElementById('eventAllDay').checked = event.allDay;
        document.getElementById('btnDeleteEvent').style.display = 'block';
        document.getElementById('eventModalTitle').textContent = 'Edit Event';

        document.getElementById('eventStart').value = this.toLocalISOString(new Date(event.start));
        document.getElementById('eventEnd').value = this.toLocalISOString(new Date(event.end));

        const modal = new bootstrap.Modal(document.getElementById('eventModal'));
        modal.show();
    },

    saveEvent: function () {
        const id = document.getElementById('eventId').value;
        const subject = document.getElementById('eventSubject').value;
        const start = document.getElementById('eventStart').value;
        const end = document.getElementById('eventEnd').value;
        const location = document.getElementById('eventLocation').value;
        const description = document.getElementById('eventDescription').value;
        const allDay = document.getElementById('eventAllDay').checked;

        if (!subject || !start || !end) {
            alert('Please fill required fields');
            return;
        }

        const data = {
            id: parseInt(id),
            subject: subject,
            startDateTime: start,
            endDateTime: end,
            location: location,
            description: description,
            isAllDay: allDay
        };

        fetch('/Calendar/SaveEvent', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(data)
        })
            .then(res => {
                if (res.ok) {
                    bootstrap.Modal.getInstance(document.getElementById('eventModal')).hide();
                    this.loadEvents();
                } else {
                    alert('Failed to save event');
                }
            });
    },

    deleteEvent: function () {
        const id = document.getElementById('eventId').value;
        if (!confirm('Are you sure you want to delete this event?')) return;

        fetch(`/Calendar/DeleteEvent?id=${id}`, { method: 'POST' })
            .then(res => {
                if (res.ok) {
                    bootstrap.Modal.getInstance(document.getElementById('eventModal')).hide();
                    this.loadEvents();
                } else {
                    alert('Failed to delete event');
                }
            });
    },

    toLocalISOString: function (date) {
        const pad = (n) => n < 10 ? '0' + n : n;
        return date.getFullYear() +
            '-' + pad(date.getMonth() + 1) +
            '-' + pad(date.getDate()) +
            'T' + pad(date.getHours()) +
            ':' + pad(date.getMinutes());
    }
};

document.addEventListener('DOMContentLoaded', () => {
    calendar.init();
});
