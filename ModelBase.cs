using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace FiZZ;

public abstract class ModelBase
{
    public event PropertyChangedEventHandler? PropertyChanged;
    protected virtual void NotifyPropertyChanged([CallerMemberName] string? propertyName = null) => 
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}