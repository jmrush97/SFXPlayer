var WebApp = function () {
    var ws;
    if ("WebSocket" in window) {

        // Let us open a web socket
        ws = new WebSocket("ws://" + location.hostname + ":3030", "ws-SFX-protocol");

        ws.onmessage = function (evt) {
            var received_msg = evt.data;
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
                            } else if (nodeName === "PlayingVolume") {
                                var pv = document.getElementById("PlayingVolume");
                                if (pv) pv.textContent = nodeValue;
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
                            } else {
                                var field = document.getElementById(nodeName);
                                if (field != null) {
                                    field.textContent = nodeValue;
                                } else {
                                    console.log("Unable to locate id=" + nodeName + ". New value = " + nodeValue);
                                }
                            }
                        }
                    }
                }
                updateProgress(posSeconds, durSeconds);
            }
        };

        ws.onclose = function () {
            // websocket is closed.
        };
    } else {
        // The browser doesn't support WebSocket
        alert("WebSocket is not supported by your browser!");
    }
    this.sendCommand = function (command) {
        if (ws && ws.readyState == WebSocket.OPEN) {
            ws.send("<command>" + command + "</command>");
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
    ctx.strokeStyle = "rgba(100, 200, 100, 0.8)";
    ctx.lineWidth = 1;
    ctx.beginPath();
    for (var i = 0; i < count; i++) {
        var x = (i / count) * w;
        var halfH = _waveformPeaks[i] * mid * 0.9;
        ctx.moveTo(x, mid - halfH);
        ctx.lineTo(x, mid + halfH);
    }
    ctx.stroke();

    // Overlay fade-in region (dark gradient from left edge)
    if (_cueFadeInMs > 0 && _trackDurationSeconds > 0) {
        var fadeInPct = Math.min(1, (_cueFadeInMs / 1000) / _trackDurationSeconds);
        var fadeInWidth = fadeInPct * w;
        if (fadeInWidth > 1) {
            var grad = ctx.createLinearGradient(0, 0, fadeInWidth, 0);
            grad.addColorStop(0, "rgba(0,0,0,0.78)");
            grad.addColorStop(1, "rgba(0,0,0,0)");
            ctx.fillStyle = grad;
            ctx.fillRect(0, 0, fadeInWidth, h);
        }
    }

    // Overlay fade-out region (dark gradient toward right edge)
    if (_cueFadeOutMs > 0 && _trackDurationSeconds > 0) {
        var fadeOutPct = Math.min(1, (_cueFadeOutMs / 1000) / _trackDurationSeconds);
        var fadeOutWidth = fadeOutPct * w;
        if (fadeOutWidth > 1) {
            var fadeOutStart = w - fadeOutWidth;
            var grad2 = ctx.createLinearGradient(fadeOutStart, 0, w, 0);
            grad2.addColorStop(0, "rgba(0,0,0,0)");
            grad2.addColorStop(1, "rgba(0,0,0,0.78)");
            ctx.fillStyle = grad2;
            ctx.fillRect(fadeOutStart, 0, fadeOutWidth, h);
        }
    }
}

function updateWaveformPosition(posSeconds, durSeconds) {
    var line = document.getElementById("waveformPositionLine");
    if (!line) return;
    if (durSeconds > 0) {
        var pct = Math.min(100, (posSeconds / durSeconds) * 100);
        line.style.left = pct.toFixed(2) + "%";
        line.style.display = "block";
    } else {
        line.style.display = "none";
    }
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
    row.style.color = window._isPlaying ? "#8f8" : "#666";
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

document.addEventListener('DOMContentLoaded', init);

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

