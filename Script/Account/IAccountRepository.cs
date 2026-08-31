using UnityEngine;

public interface IAccountRepository
{
    void Save(AccountData data);
    AccountData Load();
}