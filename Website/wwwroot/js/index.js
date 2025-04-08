window.getTitle = () => {
    return document.title;
};
window.setCulture = function (culture) {
    document.cookie = ".AspNetCore.Culture=c=" + culture + "|uic=" + culture + "; path=/";
    location.reload();
}
window.appendSizeToDiv = (id, x, y) => {
    var e = document.getElementById(id);
    var w = parseInt(e.style.width);
    var h = parseInt(e.style.height);
    e.style.width = (w + x) + 'px';
    e.style.height = (h + y) + 'px';
}
window.getHost = () => {
    return window.location.host;
}
/*window.addMathjax = () => {
    var my_awesome_script = document.createElement('script');
    console.log("added");
    my_awesome_script.setAttribute('src', 'https://cdn.jsdelivr.net/npm/mathjax@3/es5/tex-mml-chtml.js');

    document.head.appendChild(my_awesome_script);
}*/
window.clearMathJax = () => {
    MathJax.typesetClear(); // Очищает старые формулы
};

window.openNewWindow = (url, width, height) => {
    window.open(url, "_blank", `width=${width},height=${height},resizable=yes,scrollbars=yes`);
};
