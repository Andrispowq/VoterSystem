export function renderVotePieChart(chartId, labels, data) {
    const ctx = document.getElementById(chartId).getContext("2d");

    const chart = new Chart(ctx, {
        type: "pie",
        data: {
            labels: labels,
            datasets: [{
                label: "Votes",
                data: data,
                backgroundColor: [
                    "#4CAF50", "#FF6384", "#36A2EB", "#FFCE56", "#9C27B0", "#FF9800", "#607D8B"
                ],
                borderWidth: 1
            }]
        },
        options: {
            responsive: true,
            plugins: {
                legend: {
                    position: "bottom"
                }
            }
        }
    });

    return {
        destroy: () => {
            chart.destroy();
        }
    };
}