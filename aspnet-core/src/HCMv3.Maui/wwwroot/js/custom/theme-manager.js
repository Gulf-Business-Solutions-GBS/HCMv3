var BlazorThemeManagerService = function () {
    var setTheme = function (theme) {
        document.documentElement.setAttribute('data-bs-theme', theme);
        
        // Save the theme to the local storage
        localStorage.setItem('theme', theme);
    };
    
    return {
        setTheme: setTheme
    };
}();