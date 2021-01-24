using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public class CoroutineWithData<T>
{
    public T Value { get; set; }
    public IEnumerator Coroutine { get; }

    public CoroutineWithData(IEnumerator coroutine)
    {
        Coroutine = coroutine;
    }
}

