// AERO-GCS Custom SVG & HTML Diagnostics Charting Layer (ES5 Compatible)

var Charts = {
    /**
     * Renders the horizontal step-down waterfall bar diagnostic panel.
     * @param {HTMLElement} container - The container where the waterfall will be rendered.
     * @param {Object} data - The estimation response containing waterfall min values and factors.
     */
    renderWaterfall: function(container, data) {
        if (!container) return;
        container.innerHTML = '';

        var nameplate = data.nameplate_time_min;
        if (!nameplate || nameplate <= 0) {
            container.innerHTML = '<div class="text-center text-dim italic-mono">Invalid telemetry inputs.</div>';
            return;
        }

        // Define the stages
        var stages = [
            {
                label: "1. Baseline Nameplate Time",
                time: data.nameplate_time_min,
                factor: 1.0,
                desc: "100% capacity at theoretical hover draw"
            },
            {
                label: "2. Usable Battery Limit",
                time: data.usable_time_min,
                factor: data.usable_time_min / data.nameplate_time_min,
                desc: "Accounting for reserve limit and discharge efficiency"
            },
            {
                label: "3. Temperature Derated",
                time: data.temp_derated_time_min,
                factor: data.temp_derate_factor,
                desc: "Capacity loss due to LiPo battery pack temperature"
            },
            {
                label: "4. Wind Adjusted Limit",
                time: data.wind_adjusted_time_min,
                factor: 1.0 / data.wind_penalty_factor,
                desc: "Increased motor current required to fight wind drag"
            },
            {
                label: "5. Altitude Density Adjusted",
                time: data.final_estimated_time_min,
                factor: 1.0 / data.altitude_penalty_factor,
                desc: "Final flight endurance under atmospheric conditions"
            }
        ];

        for (var idx = 0; idx < stages.length; idx++) {
            var stage = stages[idx];
            var row = document.createElement('div');
            row.className = 'waterfall-row';
            
            // Format percentage of nameplate remaining
            var pctRemaining = Math.round((stage.time / nameplate) * 100);
            
            // Calculate absolute difference from previous stage
            var lossStr = '';
            var lossPct = 0;
            
            if (idx > 0) {
                var prevTime = stages[idx - 1].time;
                var diff = prevTime - stage.time;
                if (diff > 0.01) {
                    lossStr = '-' + diff.toFixed(1) + 'm';
                    lossPct = ((diff / nameplate) * 100);
                }
            }

            // Create inner HTML
            var lossBarHtml = lossPct > 0 ? '<div class="waterfall-bar-loss" style="width: ' + lossPct + '%"></div>' : '';
            var lossLabel = lossStr ? ' (' + lossStr + ')' : '';
            
            row.innerHTML = 
                '<div class="waterfall-row-info">' +
                    '<span class="waterfall-row-label">' + stage.label + '</span>' +
                    '<span class="waterfall-row-metrics">' +
                        '<span class="waterfall-time">' + stage.time.toFixed(1) + 'm</span>' +
                        '<span class="waterfall-diff">' + pctRemaining + '%' + lossLabel + '</span>' +
                    '</span>' +
                '</div>' +
                '<div class="waterfall-bar-track" title="' + stage.desc + '">' +
                    '<div class="waterfall-bar-fill" style="width: ' + ((stage.time / nameplate) * 100) + '%"></div>' +
                    lossBarHtml +
                '</div>';
            container.appendChild(row);
        }
    },

    /**
     * Renders a responsive scatter plot for Predicted vs. Actual validation.
     * @param {SVGSVGElement} svg - The SVG element to draw in.
     * @param {Array} logs - Array of flight logs.
     * @param {Function} predictor - Function that calculates predicted time given (log, coefficients).
     * @param {Object} coeffs - Current tuning coefficients {temp_coeff, wind_coeff, alt_coeff}.
     */
    renderScatterPlot: function(svg, logs, predictor, coeffs) {
        if (!svg) return;
        svg.innerHTML = ''; // Clear SVG

        var width = 320;
        var height = 220;
        var padding = { top: 15, right: 15, bottom: 30, left: 35 };

        // 1. Calculate prediction values for all logs
        var dataset = [];
        for (var li = 0; li < logs.length; li++) {
            var log = logs[li];
            var pred = predictor(log, coeffs);
            dataset.push({
                actual: log.actual_flight_time,
                predicted: pred,
                date: log.flight_date,
                payload: log.payload
            });
        }

        // 2. Find domain limits
        var maxVal = 20; // Default max limit for empty state
        if (dataset.length > 0) {
            var vals = [];
            for (var di = 0; di < dataset.length; di++) {
                vals.push(dataset[di].actual);
                vals.push(dataset[di].predicted);
            }
            maxVal = Math.max.apply(null, vals.concat([10])); // Minimum scale of 10m
            maxVal = Math.ceil(maxVal / 5) * 5; // Round to nearest 5 for clean ticks
        }

        // Helper functions to project coordinates
        var scaleX = function(val) {
            return padding.left + (val / maxVal) * (width - padding.left - padding.right);
        };
        var scaleY = function(val) {
            return height - padding.bottom - (val / maxVal) * (height - padding.top - padding.bottom);
        };

        // 3. Draw grid and axes
        var gridGroup = document.createElementNS("http://www.w3.org/2000/svg", "g");
        gridGroup.setAttribute("stroke", "#1c273e");
        gridGroup.setAttribute("stroke-width", "1");
        
        var tickCount = 4;
        for (var i = 0; i <= tickCount; i++) {
            var val = (maxVal / tickCount) * i;
            var x = scaleX(val);
            var y = scaleY(val);

            // Vertical grid lines
            var vLine = document.createElementNS("http://www.w3.org/2000/svg", "line");
            vLine.setAttribute("x1", x);
            vLine.setAttribute("y1", scaleY(0));
            vLine.setAttribute("x2", x);
            vLine.setAttribute("y2", scaleY(maxVal));
            if (i === 0) vLine.setAttribute("stroke", "#243354"); // Y axis
            gridGroup.appendChild(vLine);

            // Horizontal grid lines
            var hLine = document.createElementNS("http://www.w3.org/2000/svg", "line");
            hLine.setAttribute("x1", scaleX(0));
            hLine.setAttribute("y1", y);
            hLine.setAttribute("x2", scaleX(maxVal));
            hLine.setAttribute("y2", y);
            if (i === 0) hLine.setAttribute("stroke", "#243354"); // X axis
            gridGroup.appendChild(hLine);

            // X Ticks
            var xText = document.createElementNS("http://www.w3.org/2000/svg", "text");
            xText.setAttribute("x", x);
            xText.setAttribute("y", height - padding.bottom + 14);
            xText.setAttribute("fill", "#70849f");
            xText.setAttribute("font-size", "8");
            xText.setAttribute("font-family", "JetBrains Mono");
            xText.setAttribute("text-anchor", "middle");
            xText.textContent = '' + Math.round(val);
            svg.appendChild(xText);

            // Y Ticks
            if (i > 0) {
                var yText = document.createElementNS("http://www.w3.org/2000/svg", "text");
                yText.setAttribute("x", padding.left - 6);
                yText.setAttribute("y", y + 3);
                yText.setAttribute("fill", "#70849f");
                yText.setAttribute("font-size", "8");
                yText.setAttribute("font-family", "JetBrains Mono");
                yText.setAttribute("text-anchor", "end");
                yText.textContent = '' + Math.round(val);
                svg.appendChild(yText);
            }
        }
        svg.appendChild(gridGroup);

        // 4. Draw labels
        // X Label
        var xLabel = document.createElementNS("http://www.w3.org/2000/svg", "text");
        xLabel.setAttribute("x", padding.left + (width - padding.left - padding.right) / 2);
        xLabel.setAttribute("y", height - 4);
        xLabel.setAttribute("fill", "#70849f");
        xLabel.setAttribute("font-size", "8");
        xLabel.setAttribute("font-family", "Inter");
        xLabel.setAttribute("text-anchor", "middle");
        xLabel.setAttribute("font-weight", "500");
        xLabel.textContent = "PREDICTED ENDURANCE (MIN)";
        svg.appendChild(xLabel);

        // Y Label
        var yLabel = document.createElementNS("http://www.w3.org/2000/svg", "text");
        yLabel.setAttribute("x", -(padding.top + (height - padding.top - padding.bottom) / 2));
        yLabel.setAttribute("y", 10);
        yLabel.setAttribute("fill", "#70849f");
        yLabel.setAttribute("font-size", "8");
        yLabel.setAttribute("font-family", "Inter");
        yLabel.setAttribute("text-anchor", "middle");
        yLabel.setAttribute("font-weight", "500");
        yLabel.setAttribute("transform", "rotate(-90)");
        yLabel.textContent = "ACTUAL ENDURANCE (MIN)";
        svg.appendChild(yLabel);

        // 5. Draw Ideal Fit Line (1:1 Regression line: y = x)
        var idealLine = document.createElementNS("http://www.w3.org/2000/svg", "line");
        idealLine.setAttribute("x1", scaleX(0));
        idealLine.setAttribute("y1", scaleY(0));
        idealLine.setAttribute("x2", scaleX(maxVal));
        idealLine.setAttribute("y2", scaleY(maxVal));
        idealLine.setAttribute("stroke", "#3fd6df");
        idealLine.setAttribute("stroke-opacity", "0.25");
        idealLine.setAttribute("stroke-width", "1");
        idealLine.setAttribute("stroke-dasharray", "3,3");
        svg.appendChild(idealLine);

        // Label on ideal line
        var angleRad = -Math.atan2(height - padding.top - padding.bottom, width - padding.left - padding.right) * 180 / Math.PI;
        var lineLabel = document.createElementNS("http://www.w3.org/2000/svg", "text");
        lineLabel.setAttribute("x", scaleX(maxVal * 0.75));
        lineLabel.setAttribute("y", scaleY(maxVal * 0.75) - 4);
        lineLabel.setAttribute("fill", "#3fd6df");
        lineLabel.setAttribute("fill-opacity", "0.35");
        lineLabel.setAttribute("font-size", "7");
        lineLabel.setAttribute("font-family", "JetBrains Mono");
        lineLabel.setAttribute("transform", 'rotate(' + angleRad + ', ' + scaleX(maxVal*0.75) + ', ' + scaleY(maxVal*0.75) + ')');
        lineLabel.textContent = "IDEAL FIT (Y=X)";
        svg.appendChild(lineLabel);

        // 6. Plot Data Points
        for (var pi = 0; pi < dataset.length; pi++) {
            var d = dataset[pi];
            var cx = scaleX(d.predicted);
            var cy = scaleY(d.actual);

            // Calculate deviation error percentage
            var error = Math.abs((d.predicted - d.actual) / d.actual) * 100;
            // Color code point based on accuracy
            var pointColor = "#3fd6df";
            if (error > 15) pointColor = "#f5a623";
            if (error > 30) pointColor = "#ef5b5b";

            // Draw shadow glow circle
            var glow = document.createElementNS("http://www.w3.org/2000/svg", "circle");
            glow.setAttribute("cx", cx);
            glow.setAttribute("cy", cy);
            glow.setAttribute("r", "5");
            glow.setAttribute("fill", pointColor);
            glow.setAttribute("fill-opacity", "0.2");
            svg.appendChild(glow);

            // Draw primary point
            var point = document.createElementNS("http://www.w3.org/2000/svg", "circle");
            point.setAttribute("cx", cx);
            point.setAttribute("cy", cy);
            point.setAttribute("r", "3");
            point.setAttribute("fill", pointColor);
            point.setAttribute("stroke", "#070b13");
            point.setAttribute("stroke-width", "1");
            
            // Add a basic tooltip
            var title = document.createElementNS("http://www.w3.org/2000/svg", "title");
            title.textContent = 'Date: ' + d.date + '\nPayload: ' + d.payload.toFixed(2) + ' kg\nActual: ' + d.actual.toFixed(1) + 'm\nPredicted: ' + d.predicted.toFixed(1) + 'm\nError: ' + error.toFixed(1) + '%';
            point.appendChild(title);
            
            svg.appendChild(point);
        }
        
        // If empty dataset, render a watermark message
        if (dataset.length === 0) {
            var watermark = document.createElementNS("http://www.w3.org/2000/svg", "text");
            watermark.setAttribute("x", padding.left + (width - padding.left - padding.right) / 2);
            watermark.setAttribute("y", padding.top + (height - padding.top - padding.bottom) / 2);
            watermark.setAttribute("fill", "#223253");
            watermark.setAttribute("font-size", "10");
            watermark.setAttribute("font-family", "JetBrains Mono");
            watermark.setAttribute("text-anchor", "middle");
            watermark.textContent = "NO LOG DATA FOUND";
            svg.appendChild(watermark);
        }
    }
};
