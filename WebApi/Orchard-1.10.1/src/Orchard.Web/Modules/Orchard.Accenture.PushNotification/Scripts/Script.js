    //<![CDATA[
    var loadSessions = function () {
        var EventSelected = $('#EventsPicker :selected').val();               
        EventSelected = EventSelected == "" ? 0 : EventSelected;

        var SelectedSession = $('#SelectedSession :selected').val();
        
        //Shows the loading GIF after selecting the event
        $('#loading').show();

        //For Getting the Sessions
        //This is where the dropdownlist cascading main function
        $.ajax({
            type: "GET",
            url: "GetSessions" + "?selectedEvent=" + EventSelected,
            dataType: "json"

        }).done(function (data) {
            //Hides the loading GIF
            $('#loading').hide();

            //When succeeded get data from server construct the dropdownlist here.
            if (data != null) {
                $('#SelectedSession').empty();
                $.each(data, function (index, data) {
                    $('#SelectedSession').append('<option value="' + data.Value + '">' + data.Text + '</option>');
                });
            }
        }).fail(function (response) {
            if (response.status != 0) {
                alert(response.status + " " + response.statusText);
            }
        });

        //For Getting the Circles
        //This is where the dropdownlist cascading main function
        $.ajax({
            type: "GET",
            url: "GetCircles" + "?selectedEvent=" + EventSelected, 
            dataType: "json"

        }).done(function (data) {
            //Hides the loading GIF
            $('#loading').hide();

            //When succeeded get data from server construct the dropdownlist here.
            if (data != null) {
                $('#SelectedCircle').empty();
                $.each(data, function (index, data) {
                    $('#SelectedCircle').append('<option value="' + data.Value + '">' + data.Text + '</option>');
                });
            }
        }).fail(function (response) {
            if (response.status != 0) {
                alert(response.status + " " + response.statusText);
            }
        });
    }

    $(function () {
        //Load the Sessions and Circles of the Default Latest Event
        $('#EventsPicker').change(loadSessions);
                
        $(document).ready(function () {
            loadSessions();            
            $('#EIDs').prop('disabled', true);
            $('#SelectedCircle').prop('disabled', true);
            $('#SelectedSession').prop('disabled', true);            

            $('button[type="submit"].primaryAction').prop('disabled', true);            
            $('textarea[name="Message"]').keyup(function () {
                if ($(this).val() != '') {
                    $('button[type="submit"].primaryAction').prop('disabled', false);
                }
                else {
                    $('button[type="submit"].primaryAction').prop('disabled', true);
                }
            }); 

        });

        //If Condition for Event Wide or Single Push Notification
        $("input[name=selection]:radio").click(function () {                    
            if ($('input[name=selection]:checked').val() == "eventwide") {
                $('#EIDs').prop('disabled', true);
                $('#EventsPicker').prop('disabled', false);
                $('input[name=sessions]:radio').prop('checked', false);                        
                $('input[name=sessions]:radio').prop('disabled', false);
            } else if ($('input[name=selection]:checked').val() == "single") {
                $('#EIDs').prop('disabled', false);
                $('#EventsPicker').prop('disabled', true);
                $('#SelectedCircle').prop('disabled', true);
                $('#SelectedSession').prop('disabled', true);
                $('input[name=sessions]:radio').prop('checked', false);
                $('input[name=sessions]:radio').prop('disabled', true);
            }
        });

        //If Condition for Session or Circle Wide Push Notification
        $("input[name=sessions]:radio").click(function () {                    
            if ($('input[name=sessions]:checked').val() == "sessionwide") {
                $('#SelectedCircle').prop('disabled', true);
                $('#SelectedSession').prop('disabled', false);                       

            } else if ($('input[name=sessions]:checked').val() == "circlewide") {
                $('#SelectedSession').prop('disabled', true);
                $('#SelectedCircle').prop('disabled', false);
            }
        });

       
        
               
    })
    //]]>
