function HasHistory() {
    return window.history.length > 1;
}

function FooterResized(dotnethelper) {
    function padPageElement() {
        document.getElementsByClassName("page")[0].style.paddingBottom = footerElm.offsetHeight + "px";
    }
    var footerElm = document.getElementById("footer");
    new ResizeObserver(padPageElement).observe(footerElm);
}