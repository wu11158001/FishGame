pipeline {
    agent any

    // 定義環境變數，方便之後修改
    environment {
        // 請確保這裡的路徑與你電腦上的 Unity.exe 一致 (注意反斜線要雙寫 \\)
        UNITY_EXE = 'E:\Unity\6000.3.2f1\Editor\\Unity.exe'
        PROJECT_PATH = "${WORKSPACE}"
        BUILD_TARGET = 'WebGL'
        // 打包輸出的資料夾
        BUILD_PATH = 'Builds/WebGL'
    }

    stages {
        stage('Environment Check') {
            steps {
                echo "檢查環境中..."
                bat "if not exist \"${UNITY_EXE}\" (echo Unity Editor Not Found && exit 1)"
            }
        }

        stage('Checkout') {
            steps {
                git url: 'https://github.com/wu11158001/FishGame.git', 
                branch: 'main', 
                credentialsId: 'github-token'
            }
        }

        stage('Unity Build WebGL') {
            steps {
                echo "開始打包 WebGL..."
                // 呼叫你寫在 Assets/Editor/JenkinsBuild.cs 裡的 BuildProject 方法
                bat """
                "${UNITY_EXE}" ^
                -batchmode ^
                -quit ^
                -projectPath "${PROJECT_PATH}" ^
                -executeMethod JenkinsBuild.BuildProject ^
                -logFile "${WORKSPACE}\\unity_build_log.txt"
                """
            }
        }

        stage('Archive Results') {
            steps {
                echo "儲存成品..."
                // 將打包後的資料夾存回 Jenkins，方便下載
                archiveArtifacts artifacts: "${BUILD_PATH}/**", followSymlinks: false
                // 同時存儲 Log，方便出錯時查看
                archiveArtifacts artifacts: 'unity_build_log.txt', allowEmptyArchive: true
            }
        }
    }

    post {
        success {
            echo '✅ 打包成功！'
        }
        failure {
            echo '❌ 打包失敗，請檢查 unity_build_log.txt'
        }
    }
}