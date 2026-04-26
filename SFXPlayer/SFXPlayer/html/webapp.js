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
                            if (nodeName === "TrackPositionSeconds") {
                                posSeconds = parseFloat(nodeValue) || 0;
                            } else if (nodeName === "TrackDurationSeconds") {
                                durSeconds = parseFloat(nodeValue) || 0;
                            } else {
                                var field = document.getElementById(nodeName);
                                if (field != null) {
                                    field.innerHTML = nodeValue;
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

function updateProgress(posSeconds, durSeconds) {
    var bar = document.getElementById("progressBar");
    var label = document.getElementById("progressTime");
    if (!bar || !label) return;
    if (durSeconds > 0) {
        var pct = Math.min(100, (posSeconds / durSeconds) * 100);
        bar.style.width = pct.toFixed(1) + "%";
        var remaining = Math.max(0, durSeconds - posSeconds);
        label.textContent = formatTime(posSeconds) + " / -" + formatTime(remaining);
    } else {
        bar.style.width = "0%";
        label.textContent = "0:00 / 0:00";
    }
}

function formatTime(totalSeconds) {
    var mins = Math.floor(totalSeconds / 60);
    var secs = Math.floor(totalSeconds % 60);
    return mins + ":" + (secs < 10 ? "0" : "") + secs;
}

function init() {
    webapp = new WebApp();
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
