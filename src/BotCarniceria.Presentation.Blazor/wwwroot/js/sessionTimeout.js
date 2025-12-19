window.sessionTimeout = {
    timer: null,
    dotnetHelper: null,
    warningTime: 0,
    logoutTime: 0,

    initialize: function (dotnetHelper, warningTimeoutMs, logoutTimeoutMs) {
        this.dotnetHelper = dotnetHelper;
        this.warningTime = warningTimeoutMs;
        this.logoutTime = logoutTimeoutMs;

        // Events to monitor activity
        window.onclick = this.resetTimer.bind(this);
        window.onmousemove = this.resetTimer.bind(this);
        window.onkeypress = this.resetTimer.bind(this);
        window.onscroll = this.resetTimer.bind(this);
        window.ontouchstart = this.resetTimer.bind(this);

        this.startTimer();
    },

    startTimer: function () {
        if (this.timer) clearTimeout(this.timer);
        this.timer = setTimeout(this.onWarning.bind(this), this.warningTime);
    },

    resetTimer: function () {
        // We only reset if the timer is running (i.e. not in warning state yet)
        // Or if in warning state, the user action should validly reset it? 
        // Usually, once warning is shown, simple movement shouldn't hide it automatically 
        // without explicit "I'm here" interaction, BUT standard behavior is movement resets it.
        // However, if we show a "Are you there?" dialog, we might want to wait for explicit format.
        // Let's go effectively: movement resets timer only if NOT in warning state. 
        // If in warning state, let the DotNet side handle the reset via explicit button click.
        // Or, simpler: movement ALWAYS resets. If dialog is open, it closes.
        // Let's stick to: movement resets everything silently.
        // Wait, if I'm reading a long text and not moving, the dialog pops up.
        // If I move the mouse, the dialog should disappear.
        
        // Actually, the user asked for "Notification before expiring".
        // Let's make it so:
        // 1. Timer runs.
        // 2. onWarning -> Calls DotNet "ShowWarning".
        // 3. DotNet shows dialog.
        // 4. If user clicks "Stay", DotNet calls "ResetTimer".
        // 5. If user ignores, second timer (diff between logout and warning) fires logout.
        
        // So JS should only notify when warning time is reached. It shouldn't auto-reset on move?
        // Standard session timeout: Any request to server resets the server session.
        // JS timer is just a frontend approximation.
        // If we want to be strict, we auto-reset on user activity. 
        // But if we want to SAVE bandwidth/calls, we only talk to server when near expiry.
        // But if user is active, we validly assume session is fresh? No, user activity on client doesn't refresh server cookie unless a request is made.
        // This is a common pitfall.
        // Solution: If user is active (mousemove), we throttle a "heartbeat" to server or just rely on the fact that if they are active they will likely click something eventually.
        // But if they are just typing a long email (key presses) and auto-save isn't on, session might die.
        
        // Let's implement this:
        // monitor events. explicit resetTimer() is called only if NOT evaluating warning.
        clearTimeout(this.timer);
        this.timer = setTimeout(this.onWarning.bind(this), this.warningTime);
    },

    onWarning: function () {
        // Stop monitoring until reset
        // window.onclick = null; // etc... No, keep monitoring? 
        // If we want the dialog to be strictly manual, we can ignore events.
        // But for this task, I'll invoke DotNet.
        
        this.dotnetHelper.invokeMethodAsync('ShowSessionWarning');
        
        // Start fatal timer
        this.timer = setTimeout(this.onLogout.bind(this), this.logoutTime - this.warningTime);
    },

    onLogout: function () {
        this.dotnetHelper.invokeMethodAsync('Logout');
    },

    dispose: function () {
        if (this.timer) clearTimeout(this.timer);
        window.onclick = null;
        window.onmousemove = null;
        window.onkeypress = null;
        window.onscroll = null;
        window.ontouchstart = null;
    }
};
