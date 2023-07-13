
using Microsoft.EntityFrameworkCore;
using System.Data;
using System.Diagnostics;
using WaybackTests;

internal class Program {
    private static void Main(string[] args) {

        var prim = new Primary();
        prim.Setup();
        prim.OneToManyReversal_ExistingEntries();


        prim.OneToManyReversal_ExistingEntries();

    }
}