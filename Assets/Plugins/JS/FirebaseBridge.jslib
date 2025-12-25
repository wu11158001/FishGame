mergeInto(LibraryManager.library, {

    // 寫入新資料
    // collectionName: 集合名稱
    // docId: 資料表名稱
    // jsonData: JSON 格式的內容
    // callbackObjName: callback物件名稱
    // callbackMethod: callback方法
    SaveDataToFirestore: function (collectionName, docId, jsonData, callbackObjName, callbackMethod) {
        var collectionPath = UTF8ToString(collectionName);
        var documentPath = UTF8ToString(docId);
        var dataString = UTF8ToString(jsonData);
        var unityObj = UTF8ToString(callbackObjName);
        var callback = UTF8ToString(callbackMethod);

        try {
            var dataObject = JSON.parse(dataString);

            // 這樣如果 ID 已存在會覆蓋，不存在則建立
            window.db.collection(collectionPath).doc(documentPath).set(dataObject)
                .then(function() {
                    console.log("Firestore: 資料已成功寫入 " + collectionPath + "/" + documentPath);
                    window.unityInstance.SendMessage(unityObj, callback, "Success");
                })
                .catch(function(error) {
                    console.error("Firestore 寫入錯誤: ", error);
                    window.unityInstance.SendMessage(unityObj, callback, "Fail:" + error.code);
                });
        } catch (e) {
            console.error("JSON 解析失敗: ", e);
            window.unityInstance.SendMessage(unityObj, callback, "Error:" + e.message);
        }
    },

    // 更新資料
    // collectionName: 集合名稱
    // docId: 資料表名稱
    // jsonData: JSON 格式的內容
    // callbackObjName: callback物件名稱
    // callbackMethod: callback方法
    UpdateDataToFirestore: function (collectionName, docId, jsonData, callbackObjName, callbackMethod) {
        var collectionPath = UTF8ToString(collectionName);
        var documentPath = UTF8ToString(docId);
        var dataString = UTF8ToString(jsonData);
        var unityObj = UTF8ToString(callbackObjName);
        var callback = UTF8ToString(callbackMethod);

        try {
            var dataObject = JSON.parse(dataString);
            
            window.db.collection(collectionPath).doc(documentPath).update(dataObject)
                .then(function() {
                    console.log("Firestore: 資料已成功更新 " + collectionPath + "/" + documentPath);
                    window.unityInstance.SendMessage(unityObj, callback, "Success");
                })
                .catch(function(error) {
                    console.error("Firestore 寫更新錯誤: ", error);
                    window.unityInstance.SendMessage(unityObj, callback, "Fail:" + error.code);
                });
        } catch (e) {
            console.error("JSON 解析失敗: ", e);
            window.unityInstance.SendMessage(unityObj, callback, "Error:" + e.message);
        }
    },

    // 查詢/讀取資料
    // collectionName: 集合名稱
    // docId: 資料表名稱
    // callbackObjName: callback物件名稱
    // callbackMethod: callback方法
    GetDataFromFirestore: function (collectionName, docId, callbackObjName, callbackMethod) {
        var colPath = UTF8ToString(collectionName);
        var documentId = UTF8ToString(docId);
        var unityObj = UTF8ToString(callbackObjName);
        var callback = UTF8ToString(callbackMethod);

        window.db.collection(colPath).doc(documentId).get()
            .then(function(doc) {
                if (doc.exists) {
                    // 轉為 JSON 字串傳回
                    var data = doc.data();
                    var jsonString = JSON.stringify(data);
                    window.unityInstance.SendMessage(unityObj, callback, jsonString);
                } else {
                    // 找不到該 ID 的文件
                    window.unityInstance.SendMessage(unityObj, callback, "NotFound");
                }
            })
            .catch(function(error) {
                console.error("查詢錯誤: ", error);
                window.unityInstance.SendMessage(unityObj, callback, "Error:" + error.message);
            });
    },
});