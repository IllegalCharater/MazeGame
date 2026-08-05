using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.PlayerLoop;

public sealed class GameTimer {
    private sealed class TimerEntry {
        public int id;
        public float interval;
        public float remaining;
        public bool repeat;
        public Action callback;
        public bool canceled;
    }
    public void Init() {

    }

    private readonly List<TimerEntry> timers = new List<TimerEntry>();
    private int nextId = 1;
    private bool isTicking;

    public int Schedule(float delaySeconds, Action callback) {
        return AddTimer(delaySeconds, false, callback);
    }

    public int ScheduleRepeating(float intervalSeconds, Action callback) {
        return AddTimer(intervalSeconds, true, callback);
    }

    public bool Cancel(int id) {
        TimerEntry timer = timers.Find(entry => entry.id == id);
        if (timer == null)
            return false;

        timer.canceled = true;
        if (!isTicking)
            timers.RemoveAll(entry => entry.canceled);
        return true;
    }

    public void Tick(float deltaTime) {
        if (deltaTime <= 0f || timers.Count == 0)
            return;

        isTicking = true;
        TimerEntry[] snapshot = timers.ToArray();
        for (int i = 0; i < snapshot.Length; i++) {
            TimerEntry timer = snapshot[i];
            if (timer.canceled || !timers.Contains(timer))
                continue;

            timer.remaining -= deltaTime;
            if (timer.remaining > 0f)
                continue;

            try {
                timer.callback?.Invoke();
            }
            catch (Exception ex) {
                Debug.LogException(ex);
            }

            if (timer.canceled)
                continue;

            if (timer.repeat) {
                timer.remaining += timer.interval;
                if (timer.remaining <= 0f)
                    timer.remaining = timer.interval;
            }
            else {
                timer.canceled = true;
            }
        }

        isTicking = false;
        timers.RemoveAll(timer => timer.canceled);
    }

    public void Dispose() {
        timers.Clear();
    }

    private int AddTimer(float seconds, bool repeat, Action callback) {
        if (callback == null)
            throw new ArgumentNullException(nameof(callback));

        float interval = Math.Max(0.001f, seconds);
        TimerEntry entry = new TimerEntry {
            id = nextId++,
            interval = interval,
            remaining = interval,
            repeat = repeat,
            callback = callback
        };
        timers.Add(entry);
        return entry.id;
    }
}
