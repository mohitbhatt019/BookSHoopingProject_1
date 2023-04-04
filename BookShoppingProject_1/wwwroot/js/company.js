﻿var dataTable;
$(document).ready(function () {
    loadDataTable();
})

function loadDataTable() {
    dataTable = $('#tblData').DataTable({
        "ajax": {
            "url":"/Admin/Company/GetAll"
        },
        "columns": [
            { "data": "name", "width": "15%" },
            { "data": "streetAddress", "width": "15%" },
            { "data": "city", "width": "15%" },
            { "data": "state", "width": "15%" },
            //{ "data": "postalCode", "width": "15%" },
            { "data": "phoneNumber", "width": "15%" },
            {
                "data": "isAuthorizedCompany",
                "render": function (data) {
                    if (data) {
                        return `<input type="checkbox" checked disabled/>`;                                                  
                    }
                    else {
                        return `<input type="checkbox"  disabled/>`;
                    }
                }
            },
            {
                "data": "id",
                "render": function (data) {
                    return `
                       <div class="text-center">
                            <a href="/Admin/Company/Upsert/${data}">
                               <i class="fas fa-edit btn btn-primary"></i>
                            </a>

                            <a class="btn btn-danger" onclick=Delete("/Admin/Company/Delete/${data}")>
                                <i class="fas fa-trash-alt"></i>
                            </a>
                       </div>
                    `;
                }
            }
            
        ]
    })  
}

function Delete(url) {
    swal({
        title: "Want to delete data??",
        text: "Delete information",
        buttons: true,
        dangerModel: true,
        icon: "warning"
    }).then((willDelete) => {
        if (willDelete) {
            $.ajax({

                url: url,
                type: "Delete",
                success: function (data) {
                    if (data.success) {
                        toastr.success(data.message);
                        dataTable.ajax.reload();
                    }
                    else {
                        toastr.error(data.message);
                    }
                }
            })
        }
    })
}