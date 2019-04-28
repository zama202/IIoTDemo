function TimeStampMean(maxDateString, minDateString) {
    'use strict';

    // Convert both dates to milliseconds
    var maxDate_ms = Date.parse(maxDateString);
    var minDate_ms = Date.parse(minDateString);

    // Calculate the difference in milliseconds
    var difference_ms = maxDate_ms - minDate_ms;

    // Calculate average date
    var result = (difference_ms / 2) + (1000 * 3600);

    // var mean_date = (new Date(result)).toISOString();

    return result;
}
