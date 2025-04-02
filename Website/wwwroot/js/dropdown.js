
window.registerDropdown = function (dropdownElement, dotNetHelper) { 
    function handleClickOutside(event) {
        if (!dropdownElement.contains(event.target)) {
            dotNetHelper.invokeMethodAsync("CloseDropdown");
            document.removeEventListener("click", handleClickOutside);
        }
    }

    document.addEventListener("click", handleClickOutside);
};