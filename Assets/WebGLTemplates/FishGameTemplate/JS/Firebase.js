// Firebase 配置
const firebaseConfig = {
    apiKey: "AIzaSyCylA3UjWVJ7fgRNPzdCHb-uaNApDDA7U4",
    authDomain: "fishgame-32238.firebaseapp.com",
    projectId: "fishgame-32238",
    storageBucket: "fishgame-32238.firebasestorage.app",
    messagingSenderId: "722451418102",
    appId: "1:722451418102:web:4983bbf89eb47a2a8555d7",
    measurementId: "G-8CSR9MJ6FQ"
};

// 初始化 Firebase
firebase.initializeApp(firebaseConfig);
window.db = firebase.firestore();

console.log("Firestore 已經就緒");