# Applauncher(edit)

### 설명
http또는 ftp서버에서 version에 따라 SomeoneApp.zip을 다운로드, 압축해체 후 지정된 SomeoneApp.exe프로그램을 실행해주는 런처.

### 원본 출처
https://github.com/tom-weiland/csharp-game-launcher

### 요구사항
Visual Studio Community 2022 ( C# & WPF )
현재는 닷넷프레임워크 6.0 윈도우즈7.0으로 설정되어 있으나 필요에 따른 버전으로 설정하면됨.

### 사용법
#### App.Config
배포할 프로그램 및 서버의 정보에 맞춰 수정해서 exe와 함께 배포함.
##### SomeoneApp.zip
배포할 프로그램을 SomeoneApp을 SomeoneApp폴더를 포함하여 SomeoneApp.zip으로 만든다.
##### 서버
배포서버 (http또는 ftp)서버에 직접 업로드
##### version.txt
새로운 배포버전이 등록될때 마다 넘버링을 변경한다. 특별한 규칙은 없으며, 기존값과 다른 값을 적용하면 됨.

### 컴파일
- "배포"를 통해서 닷넷프레임워크(dll)을 포함하거나 종속할 수 있다.
- win-x64설정되어 있으며, 필요에 따라 x86, arm, osx, linux등으로 변경하도록 한다.

### 개선사항
다운로드, 해체 중에 프로그래스바와 같은 진행표시없이 단순 텍스트만 표시됨.



### DTSystemInstaller
- GameLaunchger의 배포버전을 Installer를 생성한다.
- GameLauncher를 배포 출력하고, DTSystemInstaller를 빌드하면 설치파일이 출력된다.
