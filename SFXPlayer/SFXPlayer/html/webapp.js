var WebApp = function () {
    // Custom reconnect policy: retry indefinitely with capped exponential backoff.
    // The default withAutomaticReconnect() only tries 4 times (0, 2, 10, 30 s) and
    // then permanently stops, leaving the page dead. This policy keeps retrying every
    // 5 s after the first few attempts so a brief server-side pause never kills the UI.
    var _retryPolicy = {
        nextRetryDelayInMilliseconds: function (retryContext) {
            var delays = [0, 2000, 5000, 10000];
            if (retryContext.previousRetryCount < delays.length)
                return delays[retryContext.previousRetryCount];
            return 5000; // retry every 5 s indefinitely
        }
    };

    var _connection = new signalR.HubConnectionBuilder()
        .withUrl("http://" + location.hostname + ":3030/sfxhub")
        .withAutomaticReconnect(_retryPolicy)
        .build();

    // Show connection status in the title bar when not connected
    function setConnectionStatus(text, color) {
        var el = document.getElementById("connectionStatus");
        if (!el) return;
        el.textContent = text;
        el.style.color = color || "";
        el.style.display = text ? "inline" : "none";
    }

    _connection.onreconnecting(function() {
        setConnectionStatus("Reconnecting...", "#fa8");
    });
    _connection.onreconnected(function() {
        setConnectionStatus("", "");
    });
    _connection.onclose(function(err) {
        // Should not normally reach here with the indefinite policy, but guard anyway
        setConnectionStatus("Disconnected -- refresh page", "#f55");
        if (err) console.error("SignalR connection closed with error: " + err);
    });

    function processMessage(received_msg) {
        BuildXMLFromString(received_msg);
        var DisplaySettings = xmlDoc.getElementsByTagName("DisplaySettings")[0].childNodes;
        if (DisplaySettings != null) {
            var posSeconds = 0;
            var durSeconds = 0;
            for (i = 0; i < DisplaySettings.length; i++) {
                if (DisplaySettings[i].nodeType == Node.ELEMENT_NODE) {
                    if (DisplaySettings[i + 1].nodeType == Node.TEXT_NODE) {
                        var nodeName = DisplaySettings[i].nodeName;
                        var nodeValue = DisplaySettings[i].textContent;
                        if (nodeName === "StopOthers") {
                            updateCueMode(nodeValue === "true");
                        } else if (nodeName === "TrackPositionSeconds") {
                            posSeconds = parseFloat(nodeValue) || 0;
                        } else if (nodeName === "TrackDurationSeconds") {
                            durSeconds = parseFloat(nodeValue) || 0;
                            _trackDurationSeconds = durSeconds;
                        } else if (nodeName === "CurrentVolume") {
                            var volSlider = document.getElementById("volumeSlider");
                            if (volSlider) volSlider.value = parseInt(nodeValue) || 50;
                            var volSpan = document.getElementById("CurrentVolume");
                            if (volSpan) volSpan.textContent = nodeValue;
                            var cueVolSpan = document.getElementById("CueVolume");
                            if (cueVolSpan) cueVolSpan.textContent = "Vol: " + nodeValue;
                        } else if (nodeName === "CurrentSpeed") {
                            var spd = parseFloat(nodeValue) || 1.0;
                            var spdSlider = document.getElementById("speedSlider");
                            if (spdSlider) spdSlider.value = Math.round(spd * 100);
                            var spdSpan = document.getElementById("CurrentSpeed");
                            if (spdSpan) spdSpan.textContent = spd.toFixed(2);
                        } else if (nodeName === "CueAutoRun") {
                            var isAutoRun = nodeValue === "true";
                            window._cueAutoRun = isAutoRun;
                            var btnAR = document.getElementById("btnAutoRun");
                            if (btnAR) {
                                btnAR.textContent = "Auto-run: " + (isAutoRun ? "ON" : "OFF");
                                btnAR.style.background = isAutoRun ? "#3a3" : "";
                                btnAR.style.color = isAutoRun ? "white" : "";
                            }
                            var detail = document.getElementById("CueAutoRunDetail");
                            if (detail) detail.textContent = isAutoRun ? "\u21B7 Auto" : "";
                        } else if (nodeName === "CuePauseSeconds") {
                            var ps = parseFloat(nodeValue) || 0;
                            window._cuePauseSeconds = ps;
                            var pi = document.getElementById("pauseInput");
                            if (pi) pi.value = ps.toFixed(1);
                            var detail = document.getElementById("CueAutoRunDetail");
                            if (detail && window._cueAutoRun && ps > 0) {
                                detail.textContent = "\u21B7 Auto +" + ps.toFixed(1) + "s";
                            }
                        } else if (nodeName === "CueFadeInMs") {
                            var fi = parseInt(nodeValue) || 0;
                            _cueFadeInMs = fi;
                            var fiInput = document.getElementById("fadeInInput");
                            if (fiInput) fiInput.value = fi;
                            if (_waveformPeaks) drawWaveform();
                        } else if (nodeName === "CueFadeOutMs") {
                            var fo = parseInt(nodeValue) || 0;
                            _cueFadeOutMs = fo;
                            var foInput = document.getElementById("fadeOutInput");
                            if (foInput) foInput.value = fo;
                            if (_waveformPeaks) drawWaveform();
                        } else if (nodeName === "CueFadeCurve") {
                            var cs = document.getElementById("fadeCurveSelect");
                            if (cs) cs.value = (nodeValue === "Logarithmic") ? "log" : "linear";
                        } else if (nodeName === "IsPlaying") {
                            window._isPlaying = (nodeValue === "true");
                            updatePlayingInfoVisibility();
                            updateStopAllButton();
                        } else if (nodeName === "IsPaused") {
                            window._isPaused = (nodeValue === "true");
                            updatePauseButton();
                            updatePlayingInfoVisibility();
                        } else if (nodeName === "PlayingVolume") {
                            var pv = document.getElementById("PlayingVolume");
                            if (pv) pv.textContent = nodeValue;
                            _currentVolume = parseInt(nodeValue) || 50;
                            if (_waveformPeaks) drawWaveform();
                        } else if (nodeName === "PlayingSpeed") {
                            var psp = parseFloat(nodeValue) || 1.0;
                            var psSpan = document.getElementById("PlayingSpeed");
                            if (psSpan) psSpan.textContent = psp.toFixed(2) + "x";
                        } else if (nodeName === "PlayingFadeGain") {
                            var fg = parseFloat(nodeValue);
                            if (!isNaN(fg)) updateFadeGain(fg);
                            var pfg = document.getElementById("PlayingFadeGain");
                            if (pfg) pfg.textContent = (fg * 100).toFixed(0) + "%";
                        } else if (nodeName === "AvailablePlaybackDevices") {
                            updateDeviceDropdown(nodeValue, "playbackDeviceSelect");
                        } else if (nodeName === "CurrentPlaybackDevice") {
                            var sel = document.getElementById("playbackDeviceSelect");
                            if (sel && nodeValue) {
                                for (var oi = 0; oi < sel.options.length; oi++) {
                                    if (sel.options[oi].value === nodeValue) {
                                        sel.selectedIndex = oi;
                                        break;
                                    }
                                }
                            }
                            window._currentPlaybackDevice = nodeValue;
                        } else if (nodeName === "AvailablePreviewDevices") {
                            updateDeviceDropdown(nodeValue, "previewDeviceSelect");
                        } else if (nodeName === "CurrentPreviewDevice") {
                            var psel = document.getElementById("previewDeviceSelect");
                            if (psel && nodeValue) {
                                for (var pi2 = 0; pi2 < psel.options.length; pi2++) {
                                    if (psel.options[pi2].value === nodeValue) {
                                        psel.selectedIndex = pi2;
                                        break;
                                    }
                                }
                            }
                            window._currentPreviewDevice = nodeValue;
                        } else if (nodeName === "WaveformData") {
                            updateWaveform(nodeValue || "");
                        } else if (nodeName === "CueListJson") {
                            renderCueList(nodeValue || "[]");
                        } else if (nodeName === "IsLoading") {
                            window._isLoading = (nodeValue === "true");
                            updateLoadingState();
                        } else if (nodeName === "GoTrackNum") {
                            window._goTrackNum = nodeValue;
                            updateGoButton();
                        } else if (nodeName === "GoTrackDesc") {
                            window._goTrackDesc = nodeValue;
                            updateGoButton();
                        } else if (nodeName === "ActiveTrackNum") {
                            window._activeTrackNum = nodeValue;
                            updatePauseButtonTrack();
                        } else if (nodeName === "ActiveTrackDesc") {
                            window._activeTrackDesc = nodeValue;
                            updatePauseButtonTrack();
                        } else if (nodeName === "NextNextTrackNum") {
                            window._nextNextTrackNum = nodeValue;
                            updateNextButton();
                        } else if (nodeName === "NextNextTrackDesc") {
                            window._nextNextTrackDesc = nodeValue;
                            updateNextButton();
                        } else if (nodeName === "PrevCueNumber") {
                            window._prevCueNumber = nodeValue;
                            updatePrevButton();
                            var pnField = document.getElementById("PrevCueNumber");
                            if (pnField) pnField.textContent = nodeValue;
                        } else if (nodeName === "PrevCueDescription") {
                            window._prevCueDesc = nodeValue;
                            updatePrevButton();
                            var pdField = document.getElementById("PrevCueDescription");
                            if (pdField) pdField.textContent = nodeValue;
                        } else {
                            var field = document.getElementById(nodeName);
                            if (field != null) {
                                field.textContent = nodeValue;
                                // Also update browser tab when Title changes
                                if (nodeName === "Title" && nodeValue) {
                                    document.title = nodeValue;
                                    var tab = document.getElementById("TitleTab");
                                    if (tab) tab.textContent = nodeValue;
                                }
                            } else {
                                console.log("Unable to locate id=" + nodeName + ". New value = " + nodeValue);
                            }
                        }
                    }
                }
            }
            updateProgress(posSeconds, durSeconds);
        }
    }

    _connection.on("ReceiveUpdate", processMessage);

    function startConnection() {
        _connection.start().catch(function (err) {
            console.error("SignalR connection error: " + err);
        });
    }

    startConnection();

    this.sendCommand = function (command) {
        if (_connection.state === signalR.HubConnectionState.Connected) {
            _connection.invoke("SendCommand", command).catch(function (err) {
                console.error("SignalR invoke error: " + err);
            });
        }
    }
}

function toggleAutoRun() {
    var newVal = !window._cueAutoRun;
    webapp.sendCommand("autorun:" + (newVal ? "true" : "false"));
}

function setPause() {
    var pi = document.getElementById("pauseInput");
    var secs = parseFloat(pi ? pi.value : "0") || 0;
    webapp.sendCommand("pause:" + secs.toFixed(1));
}

function setFade() {
    var fiInput = document.getElementById("fadeInInput");
    var foInput = document.getElementById("fadeOutInput");
    var csInput = document.getElementById("fadeCurveSelect");
    var fadeIn  = parseInt(fiInput  ? fiInput.value  : "0") || 0;
    var fadeOut = parseInt(foInput  ? foInput.value  : "0") || 0;
    var curve   = csInput ? csInput.value : "linear";
    webapp.sendCommand("fadein:"    + fadeIn);
    webapp.sendCommand("fadeout:"   + fadeOut);
    webapp.sendCommand("fadecurve:" + curve);
}

function deleteCue() {
    if (confirm("Delete the current next cue?")) {
        webapp.sendCommand("delete");
    }
}

function updateProgress(posSeconds, durSeconds) {
    var bar = document.getElementById("progressBar");
    var label = document.getElementById("progressTime");
    if (!bar || !label) return;
    // Store on position line for zoom re-draw
    var line = document.getElementById("waveformPositionLine");
    if (line) { line.dataset.posSeconds = posSeconds; line.dataset.durSeconds = durSeconds; }
    if (durSeconds > 0) {
        var pct = Math.min(100, (posSeconds / durSeconds) * 100);
        bar.style.width = pct.toFixed(1) + "%";
        var remaining = Math.max(0, durSeconds - posSeconds);
        label.textContent = formatTime(posSeconds) + " / -" + formatTime(remaining);
        updateWaveformPosition(posSeconds, durSeconds);
    } else {
        bar.style.width = "0%";
        label.textContent = "0:00 / 0:00";
        updateWaveformPosition(0, 0);
    }
}

function formatTime(totalSeconds) {
    var mins = Math.floor(totalSeconds / 60);
    var secs = Math.floor(totalSeconds % 60);
    return mins + ":" + (secs < 10 ? "0" : "") + secs;
}

// ---- Waveform rendering ----
var _waveformPeaks = null;
var _cueFadeInMs = 0;
var _cueFadeOutMs = 0;
var _trackDurationSeconds = 0;
var _currentVolume = 50;

// Zoom state (1 = full view; 2–8 = zoomed in on playhead)
var _waveZoom = 1.0;
var _waveZoomCenter = 0.5; // fractional center of the visible window

function computeSineFadeGain(i, count, fadeInBuckets, fadeOutBuckets) {
    if (fadeInBuckets > 0 && i < fadeInBuckets)
        return Math.sin(Math.PI / 2 * i / Math.max(1, fadeInBuckets - 1));
    if (fadeOutBuckets > 0 && i >= count - fadeOutBuckets) {
        var bucketFromEnd = count - 1 - i;
        return Math.sin(Math.PI / 2 * bucketFromEnd / Math.max(1, fadeOutBuckets - 1));
    }
    return 1.0;
}

function updateWaveform(csvData) {
    if (!csvData || csvData.length === 0) {
        _waveformPeaks = null;
        var canvas = document.getElementById("waveformCanvas");
        if (canvas) {
            var ctx = canvas.getContext("2d");
            ctx.clearRect(0, 0, canvas.width, canvas.height);
        }
        return;
    }
    var parts = csvData.split(",");
    var peaks = [];
    for (var i = 0; i < parts.length; i++) {
        var v = parseFloat(parts[i]);
        if (!isNaN(v)) peaks.push(v);
    }
    if (peaks.length === 0) return;
    _waveformPeaks = peaks;
    // Reset zoom when a new waveform loads
    _waveZoom = 1.0;
    _waveZoomCenter = 0.5;
    updateZoomLabel();
    drawWaveform();
}

function drawWaveform() {
    var canvas = document.getElementById("waveformCanvas");
    if (!canvas || !_waveformPeaks) return;
    // Sync canvas size to its CSS display size
    var rect = canvas.getBoundingClientRect();
    var w = Math.round(rect.width);
    var h = Math.round(rect.height);
    // If the canvas hasn't been laid out yet, retry on the next animation frame
    if (w < 2 || h < 2) {
        requestAnimationFrame(drawWaveform);
        return;
    }
    canvas.width = w;
    canvas.height = h;
    var ctx = canvas.getContext("2d");
    ctx.clearRect(0, 0, w, h);
    ctx.fillStyle = "#1a1a2e";
    ctx.fillRect(0, 0, w, h);
    var count = _waveformPeaks.length;
    var mid = h / 2;

    // Zoom: compute visible bucket range
    var halfWindow = 0.5 / _waveZoom;
    var startFrac = Math.max(0, _waveZoomCenter - halfWindow);
    var endFrac = Math.min(1, startFrac + 1.0 / _waveZoom);
    startFrac = endFrac - 1.0 / _waveZoom; // re-clamp after end-clamp
    var startBucket = Math.floor(startFrac * count);
    var endBucket = Math.ceil(endFrac * count);
    var visCount = Math.max(1, endBucket - startBucket);

    // Compute fade bucket counts from ms and total duration (full-track coordinates)
    var fadeInBuckets = 0;
    var fadeOutBuckets = 0;
    if (_trackDurationSeconds > 0) {
        fadeInBuckets = Math.round(Math.min(1, (_cueFadeInMs / 1000) / _trackDurationSeconds) * count);
        fadeOutBuckets = Math.round(Math.min(1, (_cueFadeOutMs / 1000) / _trackDurationSeconds) * count);
    }

    // Clamp so fade-in and fade-out regions don't overlap
    if (fadeInBuckets + fadeOutBuckets > count) {
        var total = fadeInBuckets + fadeOutBuckets;
        fadeInBuckets = Math.round(fadeInBuckets / total * count);
        fadeOutBuckets = count - fadeInBuckets;
    }

    // Pre-compute gains for all buckets
    var gains = [];
    for (var i = 0; i < count; i++) {
        gains.push(computeSineFadeGain(i, count, fadeInBuckets, fadeOutBuckets));
    }

    // Draw waveform bars for the visible (zoomed) window
    ctx.strokeStyle = "rgba(100, 200, 100, 0.8)";
    ctx.lineWidth = 1;
    ctx.beginPath();
    for (var i = startBucket; i < endBucket; i++) {
        var x = ((i - startBucket) / visCount) * w;
        var halfH = _waveformPeaks[i] * gains[i] * mid * 0.9;
        ctx.moveTo(x, mid - halfH);
        ctx.lineTo(x, mid + halfH);
    }
    ctx.stroke();

    // Draw half-sine envelope curves (upper + lower) in fade regions, clipped to visible window
    var envInStart = Math.max(startBucket, 0);
    var envInEnd   = Math.min(endBucket, fadeInBuckets);
    var envOutStart = Math.max(startBucket, count - fadeOutBuckets);
    var envOutEnd   = Math.min(endBucket, count);

    if (fadeInBuckets > 1 && envInEnd > envInStart) {
        ctx.strokeStyle = "rgba(255, 220, 0, 0.7)";
        ctx.lineWidth = 1.5;
        ctx.beginPath();
        for (var i = envInStart; i < envInEnd; i++) {
            var x = ((i - startBucket) / visCount) * w;
            var y = mid - gains[i] * mid * 0.9;
            if (i === envInStart) ctx.moveTo(x, y); else ctx.lineTo(x, y);
        }
        ctx.stroke();
        ctx.beginPath();
        for (var i = envInStart; i < envInEnd; i++) {
            var x = ((i - startBucket) / visCount) * w;
            var y = mid + gains[i] * mid * 0.9;
            if (i === envInStart) ctx.moveTo(x, y); else ctx.lineTo(x, y);
        }
        ctx.stroke();
    }

    if (fadeOutBuckets > 1 && envOutEnd > envOutStart) {
        ctx.strokeStyle = "rgba(255, 220, 0, 0.7)";
        ctx.lineWidth = 1.5;
        ctx.beginPath();
        for (var i = envOutStart; i < envOutEnd; i++) {
            var x = ((i - startBucket) / visCount) * w;
            var y = mid - gains[i] * mid * 0.9;
            if (i === envOutStart) ctx.moveTo(x, y); else ctx.lineTo(x, y);
        }
        ctx.stroke();
        ctx.beginPath();
        for (var i = envOutStart; i < envOutEnd; i++) {
            var x = ((i - startBucket) / visCount) * w;
            var y = mid + gains[i] * mid * 0.9;
            if (i === envOutStart) ctx.moveTo(x, y); else ctx.lineTo(x, y);
        }
        ctx.stroke();
    }

    // Volume level line (horizontal dashed pair at ±volume amplitude)
    if (_currentVolume > 0 && _currentVolume < 100) {
        var volH = (_currentVolume / 100) * mid * 0.9;
        ctx.strokeStyle = "rgba(100, 180, 255, 0.55)";
        ctx.lineWidth = 1;
        ctx.setLineDash([4, 4]);
        ctx.beginPath();
        ctx.moveTo(0, mid - volH); ctx.lineTo(w, mid - volH);
        ctx.moveTo(0, mid + volH); ctx.lineTo(w, mid + volH);
        ctx.stroke();
        ctx.setLineDash([]);
    }
}

function updateWaveformPosition(posSeconds, durSeconds) {
    var line = document.getElementById("waveformPositionLine");
    if (!line) return;
    if (durSeconds > 0) {
        var posFrac = posSeconds / durSeconds;
        // When zoomed, auto-follow the playhead
        if (_waveZoom > 1.0) {
            _waveZoomCenter = posFrac;
            if (_waveformPeaks) drawWaveform();
        }
        // Map position fraction to canvas percentage within zoom window
        var halfWindow = 0.5 / _waveZoom;
        var startFrac = Math.max(0, _waveZoomCenter - halfWindow);
        var endFrac = Math.min(1, startFrac + 1.0 / _waveZoom);
        startFrac = endFrac - 1.0 / _waveZoom;
        var pct = (endFrac > startFrac)
            ? ((posFrac - startFrac) / (endFrac - startFrac)) * 100
            : 50;
        pct = Math.max(0, Math.min(100, pct));
        line.style.left = pct.toFixed(2) + "%";
        line.style.display = "block";
    } else {
        line.style.display = "none";
    }
}

function updateZoomLabel() {
    var lbl = document.getElementById("waveZoomLabel");
    if (lbl) lbl.textContent = _waveZoom.toFixed(1) + "x";
}

// ---- Fade gain bar ----
function updateFadeGain(gain) {
    var fill = document.getElementById("fadeGainFill");
    if (!fill) return;
    var pct = Math.min(100, Math.max(0, gain * 100));
    fill.style.width = pct.toFixed(1) + "%";
}

// ---- Playing info visibility ----
function updatePlayingInfoVisibility() {
    var row = document.getElementById("playingInfoContent");
    if (!row) return;
    if (window._isPlaying) {
        row.style.color = "#8f8";  // green when playing
    } else if (window._isPaused) {
        row.style.color = "#fa8";  // amber when paused
    } else {
        row.style.color = "#666";  // gray when stopped
    }
}

function updateStopAllButton() {
    var btn = document.getElementById("btnStopAll");
    if (!btn) return;
    if (window._isPlaying) {
        btn.style.background = "#c33";
        btn.style.color = "white";
        btn.style.fontWeight = "bold";
    } else {
        btn.style.background = "";
        btn.style.color = "";
        btn.style.fontWeight = "";
    }
}

function updatePauseButton() {
    var btn = document.getElementById("btnPause");
    if (!btn) return;
    var labelSpan = btn.querySelector('.nav-btn-label') || btn;
    if (window._isPaused) {
        labelSpan.textContent = "Resume \u25B6";
        btn.style.background = "#27ae60";
        btn.style.fontWeight = "bold";
    } else {
        labelSpan.textContent = "Pause";
        btn.style.background = "";
        btn.style.fontWeight = "";
    }
}

// ---- Device dropdown ----
function updateDeviceDropdown(pipeSeparatedList, selectId) {
    var sel = document.getElementById(selectId || "playbackDeviceSelect");
    if (!sel) return;
    if (!pipeSeparatedList || pipeSeparatedList.length === 0) return;
    var devices = pipeSeparatedList.split("|");
    // Only rebuild if the list has changed
    var existing = [];
    for (var i = 0; i < sel.options.length; i++) {
        existing.push(sel.options[i].value);
    }
    var same = (existing.length === devices.length);
    if (same) {
        for (var j = 0; j < devices.length; j++) {
            if (existing[j] !== devices[j]) { same = false; break; }
        }
    }
    if (same) return;
    var storeKey = sel.id === "previewDeviceSelect" ? "_currentPreviewDevice" : "_currentPlaybackDevice";
    var currentDevice = window[storeKey] || (sel.options[sel.selectedIndex] ? sel.options[sel.selectedIndex].value : "");
    sel.innerHTML = "";
    for (var k = 0; k < devices.length; k++) {
        var opt = document.createElement("option");
        opt.value = devices[k];
        opt.textContent = devices[k];
        if (devices[k] === currentDevice) opt.selected = true;
        sel.appendChild(opt);
    }
}

// ---- Cue list rendering ----
function renderCueList(json) {
    var container = document.getElementById("cueListContainer");
    if (!container) return;
    var cues;
    try { cues = JSON.parse(json); } catch (e) { return; }
    if (!Array.isArray(cues)) return;
    container.innerHTML = "";
    for (var i = 0; i < cues.length; i++) {
        var c = cues[i];
        var row = document.createElement("div");
        row.className = "cue-list-item" + (c.c ? " current-cue" : "");
        var desc = c.d ? htmlEscape(c.d) : "<em style='color:#666'>—</em>";
        var file = c.f ? htmlEscape(c.f) : "";
        var cueInfo = "Vol:" + c.v + "  " + c.s + "x";
        row.innerHTML =
            "<span class='cue-num'>" + c.i + "</span>" +
            "<span class='cue-desc'>" + desc + "</span>" +
            (file ? "<span class='cue-file'>" + file + "</span>" : "") +
            "<span class='cue-meta'>" + cueInfo + "</span>";
        // Clicking a cue row moves focus to that cue without stopping playback
        (function(idx) {
            row.addEventListener("click", function() {
                webapp.sendCommand("goto:" + idx);
            });
        })(c.idx !== undefined ? c.idx : i);
        container.appendChild(row);
    }
    // Scroll the current cue into view
    var current = container.querySelector(".current-cue");
    if (current) current.scrollIntoView({ block: "nearest" });
}

function htmlEscape(s) {
    return s.replace(/&/g, "&amp;").replace(/</g, "&lt;").replace(/>/g, "&gt;").replace(/"/g, "&quot;");
}

function init() {
    webapp = new WebApp();
    window.addEventListener("resize", function() {
        if (_waveformPeaks) drawWaveform();
    });
    var canvas = document.getElementById("waveformCanvas");
    if (canvas) {
        // Click-to-seek (accounts for zoom)
        canvas.addEventListener("mousedown", function(e) {
            if (!_waveformPeaks) return;
            var rect = canvas.getBoundingClientRect();
            var clickRatio = (e.clientX - rect.left) / rect.width;
            clickRatio = Math.max(0, Math.min(1, clickRatio));
            // Map click ratio to track fraction via zoom window
            var halfWindow = 0.5 / _waveZoom;
            var startFrac = Math.max(0, _waveZoomCenter - halfWindow);
            var endFrac = Math.min(1, startFrac + 1.0 / _waveZoom);
            startFrac = endFrac - 1.0 / _waveZoom;
            var fraction = startFrac + clickRatio * (endFrac - startFrac);
            fraction = Math.max(0, Math.min(1, fraction));
            webapp.sendCommand("seek:" + fraction.toFixed(4));
        });
        // Scroll wheel to zoom in/out, centered on cursor position
        canvas.addEventListener("wheel", function(e) {
            e.preventDefault();
            var rect = canvas.getBoundingClientRect();
            var clickRatio = (e.clientX - rect.left) / rect.width;
            clickRatio = Math.max(0, Math.min(1, clickRatio));
            // Current cursor track fraction
            var halfWindow = 0.5 / _waveZoom;
            var startFrac = Math.max(0, _waveZoomCenter - halfWindow);
            var endFrac = Math.min(1, startFrac + 1.0 / _waveZoom);
            startFrac = endFrac - 1.0 / _waveZoom;
            var cursorFrac = startFrac + clickRatio * (endFrac - startFrac);
            // Adjust zoom
            if (e.deltaY < 0) {
                _waveZoom = Math.min(8, _waveZoom * 1.25);
            } else {
                _waveZoom = Math.max(1, _waveZoom / 1.25);
            }
            if (_waveZoom <= 1.0) _waveZoom = 1.0;
            // Re-center on the cursor position
            _waveZoomCenter = cursorFrac;
            updateZoomLabel();
            if (_waveformPeaks) drawWaveform();
            // Reposition the playhead line
            var line = document.getElementById("waveformPositionLine");
            if (line && line.style.display !== "none") {
                updateWaveformPosition(parseFloat(line.dataset.posSeconds || "0"),
                                       parseFloat(line.dataset.durSeconds || "0"));
            }
        }, { passive: false });
    }
}

function updateCueMode(stopOthers) {
    var modeEl = document.getElementById("CueMode");
    if (modeEl === null) return;
    if (stopOthers) {
        modeEl.className = "cue-mode stop-others";
        modeEl.innerHTML = "&#9632; Stop Others";
    } else {
        modeEl.className = "cue-mode parallel";
        modeEl.innerHTML = "&#9654; Parallel";
    }
}

function waveformZoomIn() {
    _waveZoom = Math.min(8, _waveZoom * 1.25);
    updateZoomLabel();
    if (_waveformPeaks) drawWaveform();
}

function waveformZoomOut() {
    _waveZoom = Math.max(1, _waveZoom / 1.25);
    if (_waveZoom < 1.05) { _waveZoom = 1.0; _waveZoomCenter = 0.5; }
    updateZoomLabel();
    if (_waveformPeaks) drawWaveform();
}

document.addEventListener('DOMContentLoaded', function() {
    init();
    applyStoredTheme();
});

// ---- Theme toggle ----
function applyStoredTheme() {
    var theme = localStorage.getItem('sfxTheme') || 'dark';
    document.body.classList.toggle('light-theme', theme === 'light');
    var btn = document.getElementById('btnThemeToggle');
    if (btn) btn.title = theme === 'light' ? 'Switch to dark theme' : 'Switch to light theme';
}

function toggleTheme() {
    var isLight = document.body.classList.toggle('light-theme');
    localStorage.setItem('sfxTheme', isLight ? 'light' : 'dark');
    var btn = document.getElementById('btnThemeToggle');
    if (btn) btn.title = isLight ? 'Switch to dark theme' : 'Switch to light theme';
}

// ---- Loading state ----
function updateLoadingState() {
    var loading = !!window._isLoading;
    var btnGo = document.getElementById('btnGo');
    var btnPause = document.getElementById('btnPause');
    if (btnGo) {
        btnGo.disabled = loading;
        btnGo.classList.toggle('btn-disabled', loading);
    }
    if (btnPause) {
        btnPause.disabled = loading;
        btnPause.classList.toggle('btn-disabled', loading);
    }
}

// ---- Nav button track references ----
function formatTrackReference(num, desc) {
    if (!num) return '';
    return '#' + num + (desc ? ' ' + desc : '');
}

function updateGoButton() {
    var ref = document.getElementById('goTrackRef');
    if (!ref) return;
    ref.textContent = formatTrackReference(window._goTrackNum || '', window._goTrackDesc || '');
}

function updatePauseButtonTrack() {
    var ref = document.getElementById('pauseTrackRef');
    if (!ref) return;
    ref.textContent = formatTrackReference(window._activeTrackNum || '', window._activeTrackDesc || '');
}

function updateNextButton() {
    var ref = document.getElementById('nextNextTrackRef');
    if (!ref) return;
    ref.textContent = formatTrackReference(window._nextNextTrackNum || '', window._nextNextTrackDesc || '');
}

function updatePrevButton() {
    var ref = document.getElementById('prevTrackRef');
    if (!ref) return;
    ref.textContent = formatTrackReference(window._prevCueNumber || '', window._prevCueDesc || '');
}


function CreateXMLDocument(str) {
    var xmlDoc = null;
    if (window.DOMParser) {
        var parser = new DOMParser();
        xmlDoc = parser.parseFromString(str, "text/xml");
    } else if (window.ActiveXObject) {
        xmlDoc = new ActiveXObject("Microsoft.XMLDOM");
        xmlDoc.async = false;
        xmlDoc.loadXML(str);
    }
    return xmlDoc;
}

function CreateMSXMLDocumentObject() {
    if (typeof (ActiveXObject) != "undefined") {
        var progIDs = [
                        "Msxml2.DOMDocument.6.0",
                        "Msxml2.DOMDocument.5.0",
                        "Msxml2.DOMDocument.4.0",
                        "Msxml2.DOMDocument.3.0",
                        "MSXML2.DOMDocument",
                        "MSXML.DOMDocument"
        ];
        for (var i = 0; i < progIDs.length; i++) {
            try {
                return new ActiveXObject(progIDs[i]);
            } catch (e) { };
        }
    }
    return null;
}

function BuildXMLFromString(text) {
    var message = "";
    if (window.DOMParser) { // all browsers, except IE before version 9
        var parser = new DOMParser();
        try {
            xmlDoc = parser.parseFromString(text, "text/xml");
        } catch (e) {
            return false;
        };
    }
    else {  // Internet Explorer before version 9
        xmlDoc = CreateMSXMLDocumentObject();
        if (!xmlDoc) {
            alert("Cannot create XMLDocument object");
            return false;
        }

        xmlDoc.loadXML(text);
    }

    var errorMsg = null;
    if (xmlDoc.parseError && xmlDoc.parseError.errorCode != 0) {
        errorMsg = "XML Parsing Error: " + xmlDoc.parseError.reason
                  + " at line " + xmlDoc.parseError.line
                  + " at position " + xmlDoc.parseError.linepos;
    }
    else {
        if (xmlDoc.documentElement) {
            if (xmlDoc.documentElement.nodeName == "parsererror") {
                errorMsg = xmlDoc.documentElement.childNodes[0].nodeValue;
            }
        }
        else {
            errorMsg = "XML Parsing Error!";
        }
    }

    if (errorMsg) {
        alert(errorMsg);
        return false;
    }

    return true;
}

