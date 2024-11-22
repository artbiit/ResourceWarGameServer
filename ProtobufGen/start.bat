@echo off
REM BAT 파일의 실행 디렉터리로 이동
cd /d "%~dp0"

protoc.exe --proto_path=./ --csharp_out=./ protocol.proto
