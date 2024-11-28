using Cysharp.Threading.Tasks;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Logger = ResourceWar.Server.Lib.Logger;

namespace ResourceWar.Server
{
    public static class AccountPostgreSQL
    {
        private const string TableName = "Account";

        /// <summary>
        /// Account 관련 PostgreSQL 데이터 관리 클래스
        /// </summary>
        public static async UniTask<string> GetUserNameById(int userId)
        {
            string query = $"SELECT user_name FROM {TableName} WHERE id = @id";
            return await PostgreSQLClient.Instance.ExecuteAsync(async connection =>
            {
                using (var cmd = new NpgsqlCommand(query, connection))
                {
                    cmd.Parameters.AddWithValue("id", userId);
                    var result = await cmd.ExecuteScalarAsync();
                    return result?.ToString();
                }
            });
        }

        /// <summary>
        /// 특정 유저 정보 업데이트
        /// </summary>
        /// <param name="userId">유저 ID</param>
        /// <param name="newUserName">새로운 user_name</param>
        /// <param name="newNickname">새로운 닉네임</param>
        public static async UniTask UpdateUserInfo(int userId, string newUserName, string newNickname)
        {
            string query = $"UPDATE {TableName} SET user_name = @user_name, nickname = @nickname, update_at = CURRENT_TIMESTAMP WHERE id = @id";
            await PostgreSQLClient.Instance.ExecuteAsync(async connection =>
            {
                using (var cmd = new NpgsqlCommand(query, connection))
                {
                    cmd.Parameters.AddWithValue("id", userId);
                    cmd.Parameters.AddWithValue("user_name", newUserName);
                    cmd.Parameters.AddWithValue("nickname", newNickname);
                    await cmd.ExecuteNonQueryAsync();
                }
            });
            Logger.Log($"User {userId} 정보가 업데이트되었습니다.");
        }

        /// <summary>
        /// 유저 정보 삭제
        /// </summary>
        /// <param name="userId">유저 ID</param>
        public static async UniTask DeleteUserById(int userId)
        {
            string query = $"DELETE FROM {TableName} WHERE id = @id";
            await PostgreSQLClient.Instance.ExecuteAsync(async connection =>
            {
                using (var cmd = new NpgsqlCommand(query, connection))
                {
                    cmd.Parameters.AddWithValue("id", userId);
                    await cmd.ExecuteNonQueryAsync();
                }
            });
            Logger.Log($"User {userId} 정보가 삭제되었습니다.");
        }

        /// <summary>
        /// 모든 유저 정보 가져오기
        /// </summary>
        /// <returns>유저 정보 리스트</returns>
        public static async UniTask<List<Dictionary<string, object>>> GetAllUsers()
        {
            string query = $"SELECT * FROM {TableName}";
            return await PostgreSQLClient.Instance.ExecuteAsync(async connection =>
            {
                var users = new List<Dictionary<string, object>>();
                using (var cmd = new NpgsqlCommand(query, connection))
                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        var user = new Dictionary<string, object>();
                        for (int i = 0; i < reader.FieldCount; i++)
                        {
                            user[reader.GetName(i)] = reader.GetValue(i);
                        }
                        users.Add(user);
                    }
                }
                return users;
            });
        }
    }
}
