
// setup control widget
var updateInterval = 500;
var temperatures = [3];
var ratesOfChange = [2];
var alarms = [2];
var firstIndex = -1;
var currentPage = "pageGraph";

var serviceUrl = $(document)[0].URL;
if (serviceUrl.indexOf('www/') != -1) {
    serviceUrl = serviceUrl.substr(0, serviceUrl.indexOf('www/')); 
} else {
    serviceUrl = "http://localhost:8080"; //I'm in dev
}

// setup plot
var optionsTemp = {
    series: { shadowSize: 0 }, // drawing is faster without shadows
    yaxis: { min: 0, max: 100 },
    xaxis: { show: true }
};
var optionsRate = {
    series: { shadowSize: 0 }, // drawing is faster without shadows
    yaxis: { min: -20, max: 20 },
    xaxis: { show: true }
};


$(document).ready(function(){
    jQuery.support.cors = true; // force cross-site scripting (as of jQuery 1.5)

    // we use an inline data source in the example, usually data would
    // be fetched from a server
    temperatures[0] = [];
    temperatures[1] = [];
    temperatures[2] = [];
    ratesOfChange[0] = [];
    ratesOfChange[1] = [];

    // Navigation
    $.mobile.page.prototype.options.backBtnText = "Back";
    $.mobile.page.prototype.options.addBackBtn = true;
    $.mobile.page.prototype.options.backBtnTheme = "a";

    // Page
    $.mobile.page.prototype.options.headerTheme = "a";  // Page header only
    $.mobile.page.prototype.options.contentTheme = "a";
    $.mobile.page.prototype.options.footerTheme = "a";

    poller();
});

function updateData() {
    $.ajax({
        type: "GET",
        url: serviceUrl,
        contentType: "application/json; charset=utf-8",
        dataType: "json",
        cache: false,
        success: function (msg) {
            var temps = msg.Temps; ;
            var alarms = msg.Alarms;
            // start at the correct index
            if (firstIndex == -1) {
                firstIndex = temps[0];
            }
            // only update our temps if there is new data available
            if (temps[0] >= temperatures[0].length + firstIndex) {
                temperatures[0].push([temps[0], temps[1]]);
                temperatures[1].push([temps[0], temps[2]]);
                temperatures[2].push([temps[0], temps[4]]);
                ratesOfChange[0].push([temps[0], temps[3]]);
                ratesOfChange[1].push([temps[0], temps[5]]);

                if (currentPage == "pageGraph") {
                    var plot = $.plot($("#tempGraph"), [temperatures[0], temperatures[1], temperatures[2]], optionsTemp);
                    plot.draw();
                    plot = $.plot($("#rateGraph"), [ratesOfChange[0], ratesOfChange[1]], optionsRate);
                    plot.draw();
                } else if (currentPage == "pageDisplay") {
                    $("#amb", "#pageDisplay").html(temps[1]);
                    $("#t0", "#pageDisplay").html(temps[2]);
                    $("#t1", "#pageDisplay").html(temps[4]);
                    $("#r0", "#pageDisplay").html(temps[3]);
                    $("#r1", "#pageDisplay").html(temps[5]);
                }
            }
        },      // Error!
        error: function (err) {
            console.log('oops');
        }
    });
}


function poller() {
    // since the axes don't change, we don't need to call plot.setupGrid()
    updateData();

    setTimeout(poller, updateInterval);
}


//////////////// graphPage
$(document).delegate("#pageGraph", "pageinit", function () {
    currentPage = "pageGraph";
});


//////////////// displayPage
$(document).delegate("#pageDisplay", "pageinit", function () {
    currentPage = "pageDisplay";
    $("#btnSet0").bind('click', function (e) {
        setAlarm(function () {
                $('#btnSet0').addClass('ui-disabled');
            }
        );
    });

    function setAlarm(success) {

        var params = {
            "username": $('#username').val(),
            "password": $('#password').val()
        };
        $.ajax({
            type: "POST",
            beforeSend: function () { $.mobile.showPageLoadingMsg(); }, //Show spinner
            complete: function () { $.mobile.hidePageLoadingMsg(); }, //Hide spinner
            url: "http://192.168.1.147:8080",
            contentType: "application/json; charset=utf-8",
            data: JSON.stringify(params),
            dataType: "json",
            cache: false,
            success: function () {

            },      // Error!
            error: function (err) {
                console.log('oops');
            }
        });
    }
});
   