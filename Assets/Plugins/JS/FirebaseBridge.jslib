mergeInto(LibraryManager.library, {

    // 寫入與更新資料
    // collectionName: 集合名稱
    // docId: 資料表名稱
    // jsonData: JSON 格式的內容
    // callbackObjName: callback物件名稱
    // callbackMethod: callback方法
    SaveDataToFirestore: function (collectionName, docId, jsonData, callbackObjName, callbackMethod) {
        var collectionPath = UTF8ToString(collectionName);
        var documentPath = UTF8ToString(docId);
        var dataString = UTF8ToString(jsonData);
        var unityObject = UTF8ToString(callbackObjName);
        var callback = UTF8ToString(callbackMethod);

        try {
            var dataObject = JSON.parse(dataString);
            
            // 使用 doc(documentPath).set() 
            // 這樣如果 ID 已存在會覆蓋，不存在則建立
            window.db.collection(collectionPath).doc(documentPath).set(dataObject)
                .then(function() {
                    console.log("Firestore: 資料已成功寫入 " + collectionPath + "/" + documentPath);
                    window.unityInstance.SendMessage(unityObj, callback, "success");
                })
                .catch(function(error) {
                    console.error("Firestore 寫入錯誤: ", error);
                    window.unityInstance.SendMessage(unityObj, callback, "fail");
                });
        } catch (e) {
            console.error("JSON 解析失敗: ", e);
            window.unityInstance.SendMessage(unityObj, callback, "fail");
        }
    }

});