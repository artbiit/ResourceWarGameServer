@echo off

REM 현재 디렉터리 변수 설정
set CURRENT_DIR=%~dp0

REM protoc.exe의 경로 설정
set PROTOC=%CURRENT_DIR%protoc.exe

REM 병합된 .proto 파일 이름
set MERGED_PROTO_FILE=Protocol.proto

REM 최종 결과 파일 이름
set OUTPUT_FILE=protocol.cs

REM include path 설정 (필요한 디렉터리 추가)
set INCLUDE_PATH=%CURRENT_DIR%;%CURRENT_DIR%\protobuf;%CURRENT_DIR%\protobuf\gameServer;%CURRENT_DIR%\protobuf\sessionServer

REM TEMP 폴더에 저장될 파일 리스트 초기화
set TEMP_FILE_LIST=proto_files.txt

REM 기존 작업물 삭제
echo [INFO] Cleaning up previous files...
if exist %MERGED_PROTO_FILE% del %MERGED_PROTO_FILE% /q
if exist %OUTPUT_FILE% del %OUTPUT_FILE% /q
if exist %TEMP_FILE_LIST% del %TEMP_FILE_LIST% /q

REM .proto 파일 리스트 초기화
(for /r "%CURRENT_DIR%" %%i in (*.proto) do @echo %%i) > %TEMP_FILE_LIST%

REM .proto 파일이 없을 경우 처리
if not exist %TEMP_FILE_LIST% (
    echo [ERROR] No .proto files found!
    pause
    exit /b 1
)

REM 병합된 .proto 파일의 기본 헤더 작성
(
    echo syntax = "proto3";
    echo package protocol;
    echo;
) > %MERGED_PROTO_FILE%

REM .proto 파일 병합 (중복된 syntax, package, import 제거)
for /f "delims=" %%i in (%TEMP_FILE_LIST%) do (
    for /f "usebackq delims=" %%j in ("%%i") do (
        REM 빈 줄 또는 잘못된 `echo` 방지
        if not "%%j"=="" (
            echo %%j | findstr /b /i "syntax package import" >nul
            if errorlevel 1 (
                echo %%j >> %MERGED_PROTO_FILE%
            )
        )
    )
    echo. >> %MERGED_PROTO_FILE%
)

REM `ECHO is off.` 메시지 제거
echo [INFO] Removing "ECHO is off." from the merged file...
powershell -Command "(Get-Content '%MERGED_PROTO_FILE%') -replace 'ECHO is off.', '' | Set-Content '%MERGED_PROTO_FILE%'"

REM 참조 이름 제거
echo [INFO] Removing package references from Protocol.proto...
powershell -Command ^
    "(Get-Content '%MERGED_PROTO_FILE%') | ForEach-Object { $_ -replace '\b(data\.)', '' } | Set-Content '%MERGED_PROTO_FILE%'"

REM 병합된 .proto 파일을 C# 파일로 컴파일
echo [INFO] Compiling Protocol.proto to protocol.cs...
%PROTOC% --csharp_out=%CURRENT_DIR% --proto_path=%INCLUDE_PATH% %MERGED_PROTO_FILE%
if errorlevel 1 (
    echo [ERROR] Compilation failed for %MERGED_PROTO_FILE%.
    del %TEMP_FILE_LIST% /q
    pause
    exit /b 1
)

REM 결과물 이름 확인
if exist merged.cs (
    echo [INFO] Renaming merged.cs to %OUTPUT_FILE%...
    ren merged.cs %OUTPUT_FILE%
)

REM 임시 파일 삭제
echo [INFO] Cleaning up temporary files...
del %TEMP_FILE_LIST% /q

REM 결과 확인
if exist %OUTPUT_FILE% (
    echo [SUCCESS] Compilation completed: %OUTPUT_FILE%
) else (
    echo [ERROR] Compilation failed!
)

REM 결과 확인을 위한 대기
pause

exit /b 0
