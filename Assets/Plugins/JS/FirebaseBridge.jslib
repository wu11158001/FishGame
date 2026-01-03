mergeInto(LibraryManager.library, {

    // 寫入資料
    // path: 集合名稱
    // docId: 資料表名稱
    // jsonData: JSON 格式的內容
    // callbackObj: callback物件名稱
    // callbackMethod: callback方法
    // guid: 唯一識別碼
    SaveDataToFirestore: function (path, docId, jsonData, callbackObj, callbackMethod, guid) {
        var collectionPath = UTF8ToString(path);
        var documentPath = UTF8ToString(docId);
        var dataString = UTF8ToString(jsonData);
        var unityObj = UTF8ToString(callbackObj);
        var callback = UTF8ToString(callbackMethod);
        var id = UTF8ToString(guid);

        try {
            var dataObject = JSON.parse(dataString);

            window.db.collection(collectionPath).doc(documentPath).set(dataObject)
                .then(function() {
                    var response = 
                    {
                        Guid: id,
                        IsSuccess: true,
                        Status: "Success",
                        JsonData: "", 
                    };

                    window.unityInstance.SendMessage(unityObj, callback, JSON.stringify(response));
                })
                .catch(function(error) {
                    console.error("寫入資料失敗: ", error.message);
                    var errorResp = { Guid: id, IsSuccess: false, Status: "WriteFail", JsonData: "" };
                    window.unityInstance.SendMessage(unityObj, callback, JSON.stringify(errorResp));
                });
        } catch (e) {
            console.error("JSON 解析失敗: ", e.message);
            var errorResp = { Guid: id, IsSuccess: false, Status: "Error", JsonData: "" };
            window.unityInstance.SendMessage(unityObj, callback, JSON.stringify(errorResp));
        }
    },

    // 更新資料
    // path: 集合名稱
    // docId: 資料表名稱
    // jsonData: JSON 格式的內容
    // callbackObj: callback物件名稱
    // callbackMethod: callback方法
    // guid: 唯一識別碼
    UpdateDataToFirestore: function (path, docId, jsonData, callbackObj, callbackMethod, guid) {
        var collectionPath = UTF8ToString(path);
        var documentPath = UTF8ToString(docId);
        var dataString = UTF8ToString(jsonData);
        var unityObj = UTF8ToString(callbackObj);
        var callback = UTF8ToString(callbackMethod);
        var id = UTF8ToString(guid);

        try {
            var dataObject = JSON.parse(dataString);
            
            window.db.collection(collectionPath).doc(documentPath).update(dataObject)
                .then(function() {
                    var response = 
                    {
                        Guid: id,
                        IsSuccess: true,
                        Status: "Success",
                        JsonData: "", 
                    };

                    window.unityInstance.SendMessage(unityObj, callback, JSON.stringify(response));
                })
                .catch(function(error) {
                    console.error("Firestore 更新資料錯誤: ", error.message);
                    var errorResp = { Guid: id, IsSuccess: false, Status: "UpdateFail", JsonData: "" };
                    window.unityInstance.SendMessage(unityObj, callback, JSON.stringify(errorResp));
                });
        } catch (e) {
            console.error("JSON 解析失敗: ", e.message);
            var errorResp = { Guid: id, IsSuccess: false, Status: "Error", JsonData: "" };
                    window.unityInstance.SendMessage(unityObj, callback, JSON.stringify(errorResp));
        }
    },

    // 查詢/讀取資料
    // path: 集合名稱
    // docId: 資料表名稱
    // callbackObj: callback物件名稱
    // callbackMethod: callback方法
    // guid: 唯一識別碼
    GetDataFromFirestore: function (path, docId, callbackObj, callbackMethod, guid) {
        var colPath = UTF8ToString(path);
        var documentId = UTF8ToString(docId);
        var unityObj = UTF8ToString(callbackObj);
        var callback = UTF8ToString(callbackMethod);
        var id = UTF8ToString(guid);

        window.db.collection(colPath).doc(documentId).get()
            .then(function(doc) {
                var response = 
                {
                    Guid: id,
                    IsSuccess: doc.exists,
                    Status: doc.exists ? "Success" : "AccountNotFound",
                    JsonData: doc.exists ? JSON.stringify(doc.data()) : "" 
                };

                window.unityInstance.SendMessage(unityObj, callback, JSON.stringify(response));            
            })
            .catch(function(error) {
                console.error("查詢/讀取資料: ", error.message);
                var errorResp = { Guid: id, IsSuccess: false, Status: "Error", JsonData: "" };
                window.unityInstance.SendMessage(unityObj, callback, JSON.stringify(errorResp));
            });
    },

    // 獲取集合內所有資料
    // callbackObj: Unity 回傳物件
    // callbackMethod: Unity 回傳方法
    // guid: 唯一識別碼
    GetAllDocumentsFromCollection: function (path, callbackObj, callbackMethod, guid) {
        var colPath = UTF8ToString(path);
        var unityObj = UTF8ToString(callbackObj);
        var callback = UTF8ToString(callbackMethod);
        var id = UTF8ToString(guid);

        // 使用 .get() 獲取整個集合
        window.db.collection(colPath).get()
            .then(function(querySnapshot) {
                var allData = [];
                querySnapshot.forEach(function(doc) {
                    // 將每個 doc 的數據加入陣列
                    allData.push(doc.data());
                });

                var response = {
                    Guid: id,
                    IsSuccess: true,
                    Status: "Success",
                    JsonData: JSON.stringify(allData) 
                };

                window.unityInstance.SendMessage(unityObj, callback, JSON.stringify(response));
            })
            .catch(function(error) {
                console.error("獲取集合失敗: ", error.message);
                var errorResp = { Guid: id, IsSuccess: false, Status: "Error", JsonData: "[]" };
                window.unityInstance.SendMessage(unityObj, callback, JSON.stringify(errorResp));
            });
    },

    // 刪除資料
    // path: 集合名稱
    // docId: 資料表名稱
    // callbackObj: callback物件名稱
    // callbackMethod: callback方法
    // guid: 唯一識別碼
    DeleteDataFromFirestore: function (path, docId, callbackObj, callbackMethod, guid) {
        var colPath = UTF8ToString(path);
        var documentId = UTF8ToString(docId);
        var unityObj = UTF8ToString(callbackObj);
        var callback = UTF8ToString(callbackMethod);
        var id = UTF8ToString(guid);

        window.db.collection(colPath).doc(documentId).delete()
            .then(function() {
                var response = 
                {
                    Guid: id,
                    IsSuccess: true,
                    Status: "Success",
                    JsonData: "" 
                };

                window.unityInstance.SendMessage(unityObj, callback, JSON.stringify(response));
            })
            .catch(function(error) {
                console.error("刪除資料: ", error.message);
                var errorResp = { Guid: id, IsSuccess: false, Status: "DeleteError", JsonData: "" };
                window.unityInstance.SendMessage(unityObj, callback, JSON.stringify(errorResp));
            });
    },

    // 監聽資料變更
    // path: 集合名稱
    // docId: 資料表名稱
    // callbackObj: Unity 回傳物件
    // callbackMethod: Unity 回傳方法
    ListenToFirestoreData: function (path, docId, callbackObj, callbackMethod) {
        var colPath = UTF8ToString(path);
        var documentId = UTF8ToString(docId);
        var unityObj = UTF8ToString(callbackObj);
        var callback = UTF8ToString(callbackMethod);

        // 為了之後能停止監聽，建議將 unsubscribe 函式存在 window 物件中
        // 這裡使用 documentId 作為 Key，方便辨識
        if (window.firestoreUnsubscribes === undefined) {
            window.firestoreUnsubscribes = {};
        }

        // 如果該文件已經有在監聽，先取消舊的
        if (window.firestoreUnsubscribes[documentId]) {
            window.firestoreUnsubscribes[documentId]();
        }

        // 開始監聽
        var unsub = window.db.collection(colPath).doc(documentId).onSnapshot(function(doc) {
            var response = {
                IsSuccess: doc.exists,
                Status: doc.exists ? "DataChanged" : "AccountNotFound",
                JsonData: doc.exists ? JSON.stringify(doc.data()) : ""
            };

            window.unityInstance.SendMessage(unityObj, callback, JSON.stringify(response));
        }, function(error) {
            console.error("監聽失敗: ", error.message);
            var errorResp = { Guid: id, IsSuccess: false, Status: "ListenError", JsonData: "" };
            window.unityInstance.SendMessage(unityObj, callback, JSON.stringify(errorResp));
        });

        // 儲存取消監聽的函式
        window.firestoreUnsubscribes[documentId] = unsub;
    },

    // 停止監聽
    // docId: 資料表名稱
    StopListenToFirestoreData: function (docId) {
        var documentId = UTF8ToString(docId);
        if (window.firestoreUnsubscribes && window.firestoreUnsubscribes[documentId]) {
            window.firestoreUnsubscribes[documentId]();
            delete window.firestoreUnsubscribes[documentId];
        }
    },

    // 註冊瀏覽器關閉監聽器
    // callbackObj: Unity 回傳物件
    // callbackMethod: Unity 回傳方法
    RegisterOnCloseEvent: function (callbackObj, callbackMethod) {
        window.onbeforeunload = function (e) {
            var unityObj = UTF8ToString(callbackObj);
            var callback = UTF8ToString(callbackMethod);

            SendMessage(unityObj, callback);
        };
    },
});