export function registerConnectivityListener(dotNetHelper) {
    function updateStatus() {
        dotNetHelper.invokeMethodAsync('UpdateConnectivity', navigator.onLine);
    }

    window.addEventListener('online', updateStatus);
    window.addEventListener('offline', updateStatus);

    updateStatus();
}