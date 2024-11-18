using Cysharp.Threading.Tasks;
using Npgsql;
using ResourceWar.Server;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PostgreSQLTest : MonoBehaviour
{
    private PostgreSQLClient postgresClient;
    // Start is called before the first frame update
    void Start()
    {
        TestPostgresConnection();
    }

    private async UniTask TestPostgresConnection()
    {
        postgresClient = PostgreSQLClient.Instance;

        bool isConnected = await postgresClient.ConnectAsync(
                "positivenerd.duckdns.org", // 호스트
                15004,                     // 포트
                "resourcewar",             // 데이터베이스 이름
                "resourcewar",             // 사용자 이름
                "FlthtmDnj1!"              // 비밀번호
            );

        if (isConnected)
        {
            Debug.Log("PostgreSQL 연결 성공!");

            // 샘플 데이터 삽입 및 조회 테스트
            await TestDatabaseOperations();
        }
        else
        {
            Debug.LogError("PostgreSQL 연결 실패");
        }
    }

    private async UniTask TestDatabaseOperations()
    {
        try
        {
            // 테이블 생성 (존재하지 않으면 생성)
            string createTableQuery = @"
            CREATE TABLE IF NOT EXISTS test_table (
                id SERIAL PRIMARY KEY,
                value TEXT NOT NULL
            );
        ";

            await postgresClient.ExecuteAsync(async conn =>
            {
                using (var command = new NpgsqlCommand(createTableQuery, conn))
                {
                    await command.ExecuteNonQueryAsync();
                }
            });

            Debug.Log("테이블 생성 확인 완료!");

            // 데이터 삽입
            string insertQuery = "INSERT INTO test_table (id, value) VALUES (200, 'Unity Test Value 200') ON CONFLICT (id) DO NOTHING;";
            await postgresClient.ExecuteAsync(async conn =>
            {
                using (var command = new NpgsqlCommand(insertQuery, conn))
                {
                    await command.ExecuteNonQueryAsync();
                }
            });

            Debug.Log("데이터 삽입 성공!");

            // 데이터 조회
            string selectQuery = "SELECT value FROM test_table WHERE id = 200;";
            var result = await postgresClient.ExecuteAsync(async conn =>
            {
                using (var command = new NpgsqlCommand(selectQuery, conn))
                {
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            return reader.GetString(0); // value 컬럼 값 반환
                        }
                        else
                        {
                            return "데이터 없음";
                        }
                    }
                }
            });

            Debug.Log($"조회된 데이터: {result}");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"데이터베이스 작업 중 오류 발생: {ex.Message}");
        }
    }

}
