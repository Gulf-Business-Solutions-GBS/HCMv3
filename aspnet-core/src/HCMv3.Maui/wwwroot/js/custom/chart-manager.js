// Class Definition
var BlazorChartManagerService = function () {
    var charts = {};

    var showChart = function (id, type, data, options) {
        var ctx = document.getElementById(id);

        if (!ctx) {
            console.error(`Canvas element with id ${id} not found`);
            return;
        }

        // Destroy existing chart if it exists
        if (charts[id]) {
            charts[id].destroy();
        }

        // Create new chart instance
        charts[id] = new Chart(ctx, {
            type: type,
            data: data,
            options: options
        });
    };

    var destroyChart = function (id) {
        if (charts[id]) {
            charts[id].destroy();
            delete charts[id];
        }
    };

    // Public Functions
    return {
        showChart: showChart,
        destroyChart: destroyChart
    };
}();