using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Collections.Generic;

// Clase que representa la tabla FAT
public class ArchivoFAT
{
    public string Nombre { get; set; }
    public string RutaArchivoDatosInicial { get; set; }
    public bool EnPapelera { get; set; }
    public int TamanoCaracteres { get; set; }
    public DateTime FechaCreacion { get; set; }
    public DateTime FechaModificacion { get; set; }
    public DateTime? FechaEliminacion { get; set; }

    public string Serializar()
    {
        return JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
    }

    public static ArchivoFAT Deserializar(string json)
    {
        return JsonSerializer.Deserialize<ArchivoFAT>(json);
    }
}

// Clase que representa cada bloque de datos
public class BloqueDatos
{
    public string Datos { get; set; }  // Máximo 20 caracteres
    public string SiguienteArchivo { get; set; }  // Ruta al siguiente bloque de datos
    public bool EOF { get; set; }

    public string Serializar()
    {
        return JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
    }

    public static BloqueDatos Deserializar(string json)
    {
        return JsonSerializer.Deserialize<BloqueDatos>(json);
    }
}

// Clase que maneja la lógica del sistema de archivos FAT
public class SistemaArchivosFAT
{
    private const string EXTENSION_FAT = "_FAT.json";
    private const string EXTENSION_BLOQ = ".bloq.json";
    private const string PREFIJO_BLOQUE = "bloque_";

    // Método para crear un nuevo archivo
    public void CrearArchivo()
    {
        Console.Write("Ingrese el nombre del archivo: ");
        string nombreArchivo = Console.ReadLine();
        if (string.IsNullOrWhiteSpace(nombreArchivo))
        {
            Console.WriteLine("El nombre del archivo no puede estar vacío.");
            return;
        }

        Console.WriteLine("Ingrese el contenido del archivo (presione ESC para terminar):");
        string datos = LeerDatosHastaEscape();

        ArchivoFAT nuevoArchivo = new ArchivoFAT
        {
            Nombre = nombreArchivo,
            RutaArchivoDatosInicial = "",  
            EnPapelera = false,
            TamanoCaracteres = datos.Length,
            FechaCreacion = DateTime.Now,
            FechaModificacion = DateTime.Now,
            FechaEliminacion = null
        };

        int numBloques = (int)Math.Ceiling((double)datos.Length / 20);
        string rutaAnterior = "";
        string rutaInicial = "";
        for (int i = 0; i < numBloques; i++)
        {
            int inicio = i * 20;
            int longitud = Math.Min(20, datos.Length - inicio);
            string bloqueDatos = datos.Substring(inicio, longitud);

            BloqueDatos bloque = new BloqueDatos
            {
                Datos = bloqueDatos,
                SiguienteArchivo = "",
                EOF = (i == numBloques - 1)
            };

            string rutaBloque = $"{PREFIJO_BLOQUE}{nombreArchivo}_{i}{EXTENSION_BLOQ}";
            File.WriteAllText(rutaBloque, bloque.Serializar());

            if (rutaAnterior != "")
            {
                BloqueDatos bloqueAnterior = BloqueDatos.Deserializar(File.ReadAllText(rutaAnterior));
                bloqueAnterior.SiguienteArchivo = rutaBloque;
                File.WriteAllText(rutaAnterior, bloqueAnterior.Serializar());
            }
            else
            {
                rutaInicial = rutaBloque;
                nuevoArchivo.RutaArchivoDatosInicial = rutaBloque;
            }

            rutaAnterior = rutaBloque;
        }

        File.WriteAllText($"{nombreArchivo}{EXTENSION_FAT}", nuevoArchivo.Serializar());
        Console.WriteLine("Archivo creado exitosamente.");
    }

    // Método para listar archivos
    public void ListarArchivos(bool mostrarEnPapelera = false)
    {
        var archivos = Directory.GetFiles(Directory.GetCurrentDirectory(), $"*{EXTENSION_FAT}");
        int indice = 1;
        bool hayArchivos = false;
        foreach (var archivo in archivos)
        {
            var contenido = File.ReadAllText(archivo);
            ArchivoFAT archivoFAT = ArchivoFAT.Deserializar(contenido);
            if (mostrarEnPapelera)
            {
                if (archivoFAT.EnPapelera)
                {
                    Console.WriteLine($"{indice}. Nombre: {archivoFAT.Nombre}, Tamaño: {archivoFAT.TamanoCaracteres}, Eliminado en: {archivoFAT.FechaEliminacion}");
                    indice++;
                    hayArchivos = true;
                }
            }
            else
            {
                if (!archivoFAT.EnPapelera)
                {
                    Console.WriteLine($"{indice}. Nombre: {archivoFAT.Nombre}, Tamaño: {archivoFAT.TamanoCaracteres}, Creación: {archivoFAT.FechaCreacion}, Modificación: {archivoFAT.FechaModificacion}");
                    indice++;
                    hayArchivos = true;
                }
            }
        }
        if (!hayArchivos)
        {
            if (mostrarEnPapelera)
                Console.WriteLine("No hay archivos en la papelera de reciclaje.");
            else
                Console.WriteLine("No hay archivos.");
        }
    }

    // Método para abrir y mostrar el contenido de un archivo
    public void AbrirArchivo()
    {
        var archivos = GetArchivos(false);
        if (archivos.Count == 0)
        {
            Console.WriteLine("No hay archivos para abrir.");
            return;
        }

        Console.WriteLine("Seleccione el número del archivo a abrir:");
        int seleccion = LeerSeleccion(archivos.Count);
        if (seleccion == -1)
            return;

        ArchivoFAT archivoFAT = archivos[seleccion - 1];
        Console.WriteLine($"Nombre: {archivoFAT.Nombre}, Tamaño: {archivoFAT.TamanoCaracteres}, Creación: {archivoFAT.FechaCreacion}, Modificación: {archivoFAT.FechaModificacion}");
        Console.WriteLine("Contenido del archivo:");
        string contenidoCompleto = ObtenerContenidoArchivo(archivoFAT);
        Console.WriteLine(contenidoCompleto);
    }

    // Método para modificar un archivo existente
    public void ModificarArchivo()
    {
        var archivos = GetArchivos(false);
        if (archivos.Count == 0)
        {
            Console.WriteLine("No hay archivos para modificar.");
            return;
        }

        Console.WriteLine("Seleccione el número del archivo a modificar:");
        int seleccion = LeerSeleccion(archivos.Count);
        if (seleccion == -1)
            return;

        ArchivoFAT archivoFAT = archivos[seleccion - 1];
        Console.WriteLine($"Nombre: {archivoFAT.Nombre}, Tamaño: {archivoFAT.TamanoCaracteres}, Creación: {archivoFAT.FechaCreacion}, Modificación: {archivoFAT.FechaModificacion}");
        Console.WriteLine("Contenido actual del archivo:");
        string contenidoActual = ObtenerContenidoArchivo(archivoFAT);
        Console.WriteLine(contenidoActual);

        Console.WriteLine("Ingrese el nuevo contenido del archivo (presione ESC para terminar):");
        string nuevoContenido = LeerDatosHastaEscape();

        Console.WriteLine("¿Desea guardar los cambios? (s/n): ");
        string confirmacion = Console.ReadLine();
        if (confirmacion.ToLower() != "s")
        {
            Console.WriteLine("Cambios no guardados.");
            return;
        }

        // Eliminar bloques antiguos
        string rutaBloque = archivoFAT.RutaArchivoDatosInicial;
        while (!string.IsNullOrEmpty(rutaBloque))
        {
            if (File.Exists(rutaBloque))
            {
                BloqueDatos bloque = BloqueDatos.Deserializar(File.ReadAllText(rutaBloque));
                string siguiente = bloque.SiguienteArchivo;
                File.Delete(rutaBloque);
                rutaBloque = siguiente;
            }
            else
            {
                Console.WriteLine($"Error: No se encontró el bloque de datos '{rutaBloque}'.");
                break;
            }
        }

        // Crear nuevos bloques
        archivoFAT.TamanoCaracteres = nuevoContenido.Length;
        archivoFAT.FechaModificacion = DateTime.Now;

        int numBloques = (int)Math.Ceiling((double)nuevoContenido.Length / 20);
        string rutaAnterior = "";
        string rutaInicial = "";
        for (int i = 0; i < numBloques; i++)
        {
            int inicio = i * 20;
            int longitud = Math.Min(20, nuevoContenido.Length - inicio);
            string bloqueDatos = nuevoContenido.Substring(inicio, longitud);

            BloqueDatos bloque = new BloqueDatos
            {
                Datos = bloqueDatos,
                SiguienteArchivo = "",
                EOF = (i == numBloques - 1)
            };

            string rutaBloqueNuevo = $"{PREFIJO_BLOQUE}{archivoFAT.Nombre}_{i}{EXTENSION_BLOQ}";
            File.WriteAllText(rutaBloqueNuevo, bloque.Serializar());

            if (rutaAnterior != "")
            {
                BloqueDatos bloqueAnterior = BloqueDatos.Deserializar(File.ReadAllText(rutaAnterior));
                bloqueAnterior.SiguienteArchivo = rutaBloqueNuevo;
                File.WriteAllText(rutaAnterior, bloqueAnterior.Serializar());
            }
            else
            {
                rutaInicial = rutaBloqueNuevo;
                archivoFAT.RutaArchivoDatosInicial = rutaBloqueNuevo;
            }

            rutaAnterior = rutaBloqueNuevo;
        }

        // Actualizar archivo FAT
        string rutaFAT = $"{archivoFAT.Nombre}{EXTENSION_FAT}";
        File.WriteAllText(rutaFAT, archivoFAT.Serializar());
        Console.WriteLine("Archivo modificado exitosamente.");
    }

    // Método para eliminar un archivo (mover a la papelera de reciclaje)
    public void EliminarArchivo()
    {
        var archivos = GetArchivos(false);
        if (archivos.Count == 0)
        {
            Console.WriteLine("No hay archivos para eliminar.");
            return;
        }

        Console.WriteLine("Seleccione el número del archivo a eliminar:");
        int seleccion = LeerSeleccion(archivos.Count);
        if (seleccion == -1)
            return;

        ArchivoFAT archivoFAT = archivos[seleccion - 1];
        Console.WriteLine($"¿Está seguro que desea eliminar el archivo '{archivoFAT.Nombre}'? (s/n): ");
        string confirmacion = Console.ReadLine();
        if (confirmacion.ToLower() != "s")
        {
            Console.WriteLine("Eliminación cancelada.");
            return;
        }

        archivoFAT.EnPapelera = true;
        archivoFAT.FechaEliminacion = DateTime.Now;
        string rutaFAT = $"{archivoFAT.Nombre}{EXTENSION_FAT}";
        File.WriteAllText(rutaFAT, archivoFAT.Serializar());
        Console.WriteLine("Archivo eliminado (movido a la papelera de reciclaje).");
    }

    // Método para recuperar un archivo desde la papelera de reciclaje
    public void RecuperarArchivo()
    {
        var archivos = GetArchivos(true);
        if (archivos.Count == 0)
        {
            Console.WriteLine("No hay archivos en la papelera de reciclaje para recuperar.");
            return;
        }

        Console.WriteLine("Seleccione el número del archivo a recuperar:");
        int seleccion = LeerSeleccion(archivos.Count);
        if (seleccion == -1)
            return;

        ArchivoFAT archivoFAT = archivos[seleccion - 1];
        Console.WriteLine($"¿Está seguro que desea recuperar el archivo '{archivoFAT.Nombre}'? (s/n): ");
        string confirmacion = Console.ReadLine();
        if (confirmacion.ToLower() != "s")
        {
            Console.WriteLine("Recuperación cancelada.");
            return;
        }

        archivoFAT.EnPapelera = false;
        archivoFAT.FechaEliminacion = null;
        string rutaFAT = $"{archivoFAT.Nombre}{EXTENSION_FAT}";
        File.WriteAllText(rutaFAT, archivoFAT.Serializar());
        Console.WriteLine("Archivo recuperado exitosamente.");
    }

    // Método auxiliar para obtener una lista de archivos según su estado
    private List<ArchivoFAT> GetArchivos(bool enPapelera)
    {
        var archivos = Directory.GetFiles(Directory.GetCurrentDirectory(), $"*{EXTENSION_FAT}");
        List<ArchivoFAT> lista = new List<ArchivoFAT>();
        foreach (var archivo in archivos)
        {
            var contenido = File.ReadAllText(archivo);
            ArchivoFAT archivoFAT = ArchivoFAT.Deserializar(contenido);
            if (archivoFAT.EnPapelera == enPapelera)
            {
                lista.Add(archivoFAT);
            }
        }
        return lista;
    }

    // Método auxiliar para obtener el contenido completo de un archivo
    private string ObtenerContenidoArchivo(ArchivoFAT archivoFAT)
    {
        string contenidoCompleto = "";
        string rutaBloque = archivoFAT.RutaArchivoDatosInicial;
        while (!string.IsNullOrEmpty(rutaBloque))
        {
            if (!File.Exists(rutaBloque))
            {
                Console.WriteLine($"Error: No se encontró el bloque de datos '{rutaBloque}'.");
                break;
            }

            var bloqueJson = File.ReadAllText(rutaBloque);
            BloqueDatos bloque = BloqueDatos.Deserializar(bloqueJson);
            contenidoCompleto += bloque.Datos;
            rutaBloque = bloque.SiguienteArchivo;
        }
        return contenidoCompleto;
    }

    // Método auxiliar para leer la selección del usuario
    private int LeerSeleccion(int max)
    {
        Console.Write($"Ingrese un número (1-{max}) o 0 para cancelar: ");
        string input = Console.ReadLine();
        if (int.TryParse(input, out int seleccion))
        {
            if (seleccion == 0)
            {
                Console.WriteLine("Operación cancelada.");
                return -1;
            }
            if (seleccion >= 1 && seleccion <= max)
            {
                return seleccion;
            }
        }
        Console.WriteLine("Selección inválida.");
        return -1;
    }

    // Método auxiliar para leer datos hasta que el usuario presione ESC
    private string LeerDatosHastaEscape()
    {
        string datos = "";
        ConsoleKeyInfo keyInfo;
        Console.WriteLine("Ingrese el contenido del archivo. Presione ESC para terminar:");
        while (true)
        {
            keyInfo = Console.ReadKey(intercept: true);
            if (keyInfo.Key == ConsoleKey.Escape)
            {
                break;
            }
            else if (keyInfo.Key == ConsoleKey.Enter)
            {
                datos += Environment.NewLine;
                Console.WriteLine();
            }
            else
            {
                datos += keyInfo.KeyChar;
                Console.Write(keyInfo.KeyChar);
            }
        }
        return datos;
    }
}

// Clase principal que contiene el menú interactivo
class ProgramaFAT
{
    static void Main()
    {
        bool ejecutando = true;
        SistemaArchivosFAT sistema = new SistemaArchivosFAT();

        while (ejecutando)
        {
            Console.WriteLine("\n----- Menú FAT -----");
            Console.WriteLine("1. Crear un archivo y agregar datos");
            Console.WriteLine("2. Listar archivos");
            Console.WriteLine("3. Abrir un archivo");
            Console.WriteLine("4. Modificar un archivo");
            Console.WriteLine("5. Eliminar un archivo");
            Console.WriteLine("6. Recuperar un archivo");
            Console.WriteLine("7. Salir");
            Console.Write("Seleccione una opción: ");

            string opcion = Console.ReadLine();
            Console.WriteLine();

            switch (opcion)
            {
                case "1":
                    sistema.CrearArchivo();
                    break;
                case "2":
                    sistema.ListarArchivos();
                    break;
                case "3":
                    sistema.AbrirArchivo();
                    break;
                case "4":
                    sistema.ModificarArchivo();
                    break;
                case "5":
                    sistema.EliminarArchivo();
                    break;
                case "6":
                    sistema.RecuperarArchivo();
                    break;
                case "7":
                    ejecutando = false;
                    Console.WriteLine("Saliendo del programa...");
                    break;
                default:
                    Console.WriteLine("Opción no válida. Intente nuevamente.");
                    break;
            }
        }
    }
}
