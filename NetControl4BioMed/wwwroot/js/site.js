﻿// Please see documentation at https://docs.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Wait for the window to load.
$(window).on('load', () => {

    // Define the time interval in which refreshing takes place, in miliseconds.
    const _refreshInterval = 5000;

    // Check if there is a cookie notification alert on the page.
    if ($('.cookie-consent-alert').length !== 0) {
        // Get the cookie acceptance button.
        const button = $('.cookie-consent-alert').first().find('.cookie-consent-alert-dismiss');
        // Add a listener for clicking the button.
        $(button).on('click', () => {
            document.cookie = button.data('cookie-consent-string');
        });
    }

    // Check if there is a UTC date on the page.
    if ($('.utc-date').length !== 0) {
        // Define a function to update an element containing a UTC date.
        const updateDate = (element) => {
            // Get the date in UTC format.
            const date = new Date($(element).data('date'));
            // Update the UTC date to the local date.
            $(element).find('.utc-date-date').attr('title', date.toLocaleDateString());
            $(element).find('.utc-date-date').text(date.toLocaleDateString());
            $(element).find('.utc-date-time').attr('title', date.toLocaleTimeString());
            $(element).find('.utc-date-time').text(date.toLocaleTimeString());
        };
        // Execute the function on page load.
        (() => {
            // Update all elements containing UTC dates.
            $('.utc-date').each((index, element) => updateDate(element));
        })();
    }

    // Check if there is a QR code on the page.
    if ($('.qr-code').length !== 0) {
        // Load the QR generation script.
        $.getScript('/lib/jquery-qrcode/jquery.qrcode.min.js', () => {
            // Go over all of the QR codes on the page.
            $('.qr-code').each((index, element) => {
                // And generate the QR code based on the given uri.
                $(element).qrcode($(element).data('uri'));
            });
        });
    }

    // Check if there is a datatable on the page.
    if ($('.table-datatable').length !== 0) {
        // Go over each datatable.
        $('.table-datatable').each((index, element) => {
            // Format the table as datatable.
            const table = $(element).DataTable({
                'autoWidth': false
            });
            // Get the index of the index column.
            const columnIndex = table.column('index:name').index();
            // Check if there is any index column.
            if (typeof (columnIndex) !== 'undefined') {
                // Add a listener for ordering or searching in the table.
                table.on('order.dt search.dt', () => {
                    // Update the corresponding column on searching or ordering.
                    table.column(columnIndex, { search: 'applied', order: 'applied' }).nodes().each((cell, index) => {
                        $(cell).find('span').html(index + 1);
                    });
                }).draw();
            }
        });
    }

    // Check if there is a table group on the page.
    if ($('.table-group').length !== 0) {
        // Define a function to update the input.
        const updateInput = (groupElement, rowElement) => {
            // Get the current item ID.
            const itemId = $(rowElement).data('id');
            // Deserialize the value within the input element.
            const itemIds = JSON.parse($(groupElement).find('.table-group-input').first().val());
            // Toggle the selection class on the row and on the checkbox.
            $(rowElement).toggleClass('table-active');
            // Check if the row is now selected.
            if ($(event.target).closest('.table-group-row').hasClass('table-active')) {
                // Update the checkbox.
                $(rowElement).find('.table-group-row-checkbox:not(checked)').prop('checked', true);
                // Get the current index of the item.
                const index = itemIds.indexOf(itemId);
                // Check if the item doesn't already exist.
                if (index === -1) {
                    // Add it to the list.
                    itemIds.push(itemId);
                    // Update the input element.
                    $(groupElement).find('.table-group-input').val(JSON.stringify(itemIds));
                    // Update the value of the count.
                    $(groupElement).find('.table-group-count').text(itemIds.length);
                }
            } else {
                // Update the checkbox.
                $(rowElement).find('.table-group-row-checkbox:checked').prop('checked', false);
                // Get the current index of the item.
                const index = itemIds.indexOf(itemId);
                // Check if the item exists.
                if (index !== -1) {
                    // Remove it from the list.
                    itemIds.splice(index, 1);
                    // Update the input element.
                    $(groupElement).find('.table-group-input').val(JSON.stringify(itemIds));
                    // Update the value of the count.
                    $(groupElement).find('.table-group-count').text(itemIds.length);
                }
            }
        };
        // Define a function to update the table.
        const updateTable = (groupElement) => {
            // Deserialize the value within the input element.
            const itemIds = JSON.parse($(groupElement).find('.table-group-input').first().val());
            // Update the value of the count.
            $(groupElement).find('.table-group-count').text(itemIds.length);
            // Go over each item ID.
            for (const itemId of itemIds) {
                // Mark the corresponding row as active.
                $(groupElement).find(`.table-group-row[data-id="${itemId}"]`).addClass('table-active');
                $(groupElement).find(`.table-group-row[data-id="${itemId}"]`).find('.table-group-row-checkbox').prop('checked', true);
            }
        }
        // Add a listener for clicking on a row in the table.
        $('.table-group-datatable').on('click', '.table-group-row', (event) => {
            // Get the current group and row element.
            const groupElement = $(event.target).closest('.table-group');
            const rowElement = $(event.target).closest('.table-group-row');
            // Update the input.
            updateInput(groupElement, rowElement);
        });
        // Add a listener for clicking on a checkbox in the table.
        $('.table-group-datatable').on('change', '.table-group-row-checkbox', (event) => {
            // Get the current group and row element.
            const groupElement = $(event.target).closest('.table-group');
            const rowElement = $(event.target).closest('.table-group-row');
            // Update the input.
            updateInput(groupElement, rowElement);
        });
        // On page load, parse the input and check the group items.
        (() => {
            // Go over each table group.
            $('.table-group').each((index, element) => {
                // Format the table as datatable.
                const table = $(element).find('.table-group-datatable').DataTable({
                    'autoWidth': false
                });
                // Update the table.
                updateTable($(element));
            });
        })();
    }

    // Check if there is an item group on the page.
    if ($('.item-group').length !== 0) {
        // Define a function which gets all of the selected items and creates a JSON string array with their IDs.
        const updateSelectedItems = (groupElement) => {
            // Get all of the list group items.
            const items = $(groupElement).find('.item-group-item');
            // Go over all of the checked elements and get the corresponding list group items.
            const selectedItems = $(groupElement).find('.item-group-item-checkbox:checked').closest('.item-group-item');
            // Go over all of the unchecked elements and get the corresponding list group items.
            const unselectedItems = $(groupElement).find('.item-group-item-checkbox:not(:checked)').closest('.item-group-item');
            // Go over all of the selected items and check all of the checkboxes.
            $(selectedItems).find('input[type="checkbox"]:not(:checked)').prop('checked', true);
            // Go over all of the unselected items and uncheck all of the checkboxes.
            $(unselectedItems).find('input[type="checkbox"]:checked').prop('checked', false);
            // Go over all of the selected items and mark them as active.
            $(selectedItems).addClass('table-active');
            // Go over all of the unselected items and mark them as not active.
            $(unselectedItems).removeClass('table-active');
            // Check how many elements are selected.
            if (selectedItems.length === 0) {
                // Disable the group buttons.
                $('.item-group-button').prop('disabled', true);
                // Unmark the checkbox as indeterminate.
                $(groupElement).find('.item-group-select').prop('indeterminate', false);
                // Uncheck the checkbox.
                $(groupElement).find('.item-group-select').prop('checked', false);
            } else {
                // Enable the group buttons.
                $('.item-group-button').prop('disabled', false);
                // Check if not all elements are selected.
                if (selectedItems.length !== items.length) {
                    // Mark the checkbox as indeterminate.
                    $(groupElement).find('.item-group-select').prop('indeterminate', true);
                } else {
                    // Unmark the checkbox as indeterminate.
                    $(groupElement).find('.item-group-select').prop('indeterminate', false);
                    // Check the checkbox.
                    $(groupElement).find('.item-group-select').prop('checked', true);
                }
            }
        };
        // Add a listener for when a checkbox gets checked or unchecked.
        $('.item-group').on('change', '.item-group-item-checkbox', (event) => {
            // Get the current list group.
            const groupElement = $(event.target).closest('.item-group');
            // Update the selected items.
            updateSelectedItems(groupElement);
        });
        // Add a listener for the select checkbox.
        $('.item-group-select').on('change', (event) => {
            // Get the current list group.
            const groupElement = $(event.target).closest('.item-group');
            // Check if the checkbox is currently checked.
            if ($(event.target).prop('checked')) {
                // Check all of the checkboxes on the page.
                $(groupElement).find('.item-group-item-checkbox').prop('checked', true);
            } else {
                // Uncheck all of the checkboxes on the page.
                $(groupElement).find('.item-group-item-checkbox').prop('checked', false);
            }
            // Update the selected items.
            updateSelectedItems(groupElement);
        });
        // On page load, parse the input and check the group items.
        (() => {
            // Go over all of the groups.
            $('.item-group').each((index, element) => {
                // Update the selected items.
                updateSelectedItems(element);
            });
        })();
    }

    // Check if there is a file group on the page.
    if ($('.file-group').length !== 0) {
        // Define a function which updates the data to be submitted.
        const updateInput = (groupElement) => {
            // Check if the text is empty.
            if (!$.trim($(groupElement).find('.file-group-text').first().val())) {
                // Update the value of the count.
                $(groupElement).find('.file-group-count').first().text(0);
                // Update the data to be submitted.
                $(groupElement).find('.file-group-input').first().val(JSON.stringify([]));
                // Return from the function.
                return;
            }
            // Get the in-line separator.
            const inlineSeparator = $(groupElement).find('.file-group-in-line-separator').first().val();
            // Get the line separator.
            const lineSeparator = $(groupElement).find('.file-group-line-separator').first().val();
            // Get the type of the file group.
            const type = $(groupElement).data('type');
            // Check if we have simple items.
            if (type === 'items') {
                // Split the text into different lines.
                const rows = $(groupElement).find('.file-group-text').first().val().split(new RegExp(lineSeparator)).filter((element) => {
                    // Select only the non empty elements.
                    return $.trim(element);
                });
                // Update the value of the count.
                $(groupElement).find('.file-group-count').first().text(rows.length);
                // Update the data to be submitted.
                $(groupElement).find('.file-group-input').first().val(JSON.stringify(rows));
            }
            // Check if we have pairs of items.
            else if (type === 'pairs') {
                // Split the text into different lines.
                const rows = $(groupElement).find('.file-group-text').first().val().split(new RegExp(lineSeparator)).filter((element) => {
                    // Split the row into its composing items.
                    const row = element.split(new RegExp(inlineSeparator)).filter((el) => {
                        // Select only the non-empty items.
                        return el !== '';
                    });
                    // Select only elements with more than two items.
                    return row.length === 2;
                });
                // Go over each row.
                const items = $.map(rows, (element, index) => {
                    // Split the row into its composing items.
                    const row = element.split(new RegExp(inlineSeparator));
                    // Check if we don't have both source and target nodes.
                    if (!row[0] || !row[1]) {
                        // Don't return anything.
                        return;
                    }
                    // Split the element into an array of items.
                    return { 'SourceNode': row[0], 'TargetNode': row[1] };
                });
                // Update the value of the count.
                $(groupElement).find('.file-group-count').first().text(rows.length);
                // Update the data to be submitted.
                $(groupElement).find('.file-group-input').first().val(JSON.stringify(items));
            }
        };
        // Define a function which updates the displayed text.
        const updateText = (groupElement) => {
            // Get the type of the file group.
            const type = $(groupElement).data('type');
            // Define a variable for the input data.
            let data = undefined;
            // Try to parse the input data.
            try {
                // Get the input data.
                data = JSON.parse($(groupElement).find('.file-group-input').first().val());
            }
            catch (error) {
                // Return from the function.
                return;
            }
            // Check if there isn't any data.
            if (typeof data === 'undefined') {
                // Return from the function.
                return;
            }
            // Check if we have a proper array.
            if (!Array.isArray(data)) {
                // Return from the function.
                return;
            }
            // Check if we have simple items.
            if (type === 'items') {
                // Go over all of the elements.
                data = data.filter((element) => {
                    // Keep only the ones which are of the proper type.
                    return typeof element === 'string';
                });
                // Add the elements to the text.
                $(groupElement).find('.file-group-text').first().val(data.join('\n'));
            }
            // Check if we have pairs of items.
            if (type === 'pairs') {
                // Go over all of the elements.
                data = data.filter((element) => {
                    // Keep only the ones which are of the proper type.
                    return element['SourceNode'] && typeof element['SourceNode'] === 'string' && element['TargetNode'] && typeof element['TargetNode'] === 'string';
                });
                // Go over all of the elements.
                data = $.map(data, (element, index) => {
                    // Check if we have more than one of each.
                    if (!element['SourceNode'] || !element['TargetNode']) {
                        // Return an undefined value.
                        return undefined;
                    } else {
                        // Return a simplified object.
                        return `${element['SourceNode']};${element['TargetNode']}`;
                    }
                });
                // Add the elements to the text.
                $(groupElement).find('.file-group-text').first().val(data.join('\n'));
            }
        };
        // Add a listener for typing into the text area.
        $('.file-group-text').on('keyup', (event) => {
            // Get the actual group which was clicked.
            const groupElement = $(event.target).closest('.file-group');
            // Update the selected items.
            updateInput(groupElement);
        });
        // Add a listener for changing one of the separators.
        $('.file-group').on('change', '.file-group-separator', (event) => {
            // Get the actual group which was clicked.
            const groupElement = $(event.target).closest('.file-group');
            // Update the selected items.
            updateInput(groupElement);
        });
        // Add a listener for if the upload button was clicked.
        $('.file-group-file-upload').on('change', (event) => {
            // Get the current file.
            const file = event.target.files[0];
            // Set the filename in the label.
            $(event.target).siblings('.file-group-file-label').html(file.name);
            // Define the file reader and the variable for storing its content.
            let fileReader = new FileReader();
            // Define what happens when we read the file.
            fileReader.onload = (e) => {
                // Write the content of the file to the text area.
                $(event.target).closest('.file-group').find('.file-group-text').first().val(e.target.result);
                // Get the actual group which was clicked.
                const groupElement = $(event.target).closest('.file-group');
                // Update the selected items.
                updateInput(groupElement);
            };
            // Read the file.
            fileReader.readAsText(file);
        });
        // Execute the function on page load.
        (() => {
            // Go over all of the groups.
            $('.file-group').each((index, groupElement) => {
                // Update the text.
                updateText(groupElement);
                // Update the selected items.
                updateInput(groupElement);
            });
        })();
    }

    // Check if there is a Cytoscape area on the page.
    if ($('.cytoscape-area').length !== 0) {
        // Get the Cytoscape configuration JSON.
        const cytoscapeJson = JSON.parse($('.cytoscape-configuration').first().text());
        // Define the Cytoscape variable.
        const cy = cytoscape({
            container: $('.cytoscape-container').first().get(0),
            elements: cytoscapeJson.elements,
            layout: cytoscapeJson.layout,
            style: cytoscapeJson.style
        });
        // Add listener for when a node is clicked.
        cy.on('tap', 'node', (event) => {
            // Check if there is a link.
            if (event.target.data('href') && event.target.data('href').length !== 0) {
                // Open a new link.
                window.location.href = event.target.data('href');
            }
        });
        // Hide the loading message.
        $('.cytoscape-loading').prop('hidden', true);
    }

    // Check if there is a refreshable item on the page.
    if ($('.item-refresh').length !== 0) {
        // Define a function to refresh the details.
        const refresh = (element) => {
            // Get the ID of the item.
            const id = $(element).data('id');
            // Get the status of the item.
            const status = $(element).data('status');
            // Get the data for the item with the provided ID.
            const ajaxCall = $.ajax({
                url: `${window.location.pathname}?handler=Refresh&id=${id}`,
                dataType: 'json',
                success: (data) => {
                    // Check if the status has changed.
                    if (status !== data.status) {
                        // Reload the page.
                        location.reload(true);
                    }
                    // Go over each JSON property.
                    $.each(data, (key, value) => {
                        // Update the corresponding fields.
                        $(element).find(`.item-refresh-item[data-type=${key}]`).attr('title', value);
                        $(element).find(`.item-refresh-item[data-type=${key}]`).text(value);
                    });
                },
                error: () => { }
            });
        };
        // Execute the function on page load.
        (() => {
            // Refresh all items once.
            $('.item-refresh').each((index, element) => refresh(element));
            // Check if the items need to be refreshed.
            if ($('.item-refresh[data-refresh="True"]').length !== 0) {
                // Repeat the function every few seconds.
                setInterval(() => {
                    // Go over all elements in the page.
                    $('.item-refresh[data-refresh="True"]').each((index, element) => refresh(element));
                }, _refreshInterval);
            }
        })();
    }

});
