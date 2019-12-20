module App

open Fable.Core.JsInterop
open Fable.React
open Fable.MaterialUI.Icons
open Fable.MaterialUI.MaterialDesignIcons
open Feliz
open Feliz.MaterialUI
open Feliz.Router
open MarkdonViewer


[<RequireQualifiedAccess>]
module Url =

  let [<Literal>] pages = "pages"
  let [<Literal>] usage = "usage"
  let [<Literal>] ecosystem = "ecosystem"
  let [<Literal>] components = "components"
  let [<Literal>] installation = "installation"
  let [<Literal>] components_props = "components-props"
  let [<Literal>] classes = "classes"
  let [<Literal>] styling = "styling"
  let [<Literal>] themes = "themes"
  let [<Literal>] hooks = "hooks"
  let [<Literal>] autocomplete = "autocomplete"
  let [<Literal>] indexMd = "index.md"


type ThemeMode =
  | Light
  | Dark


type Model = {
  CurrentPath: string list
  CustomThemeMode: ThemeMode option
}


type Msg =
  | SetPath of string list
  | ToggleCustomThemeMode


let init () = {
  CurrentPath = []
  CustomThemeMode = None
}


let update (msg: Msg) (m: Model) =
  match msg with
  | SetPath segments ->
      { m with CurrentPath = segments }
  | ToggleCustomThemeMode ->
      { m with
          CustomThemeMode =
            match m.CustomThemeMode with
            | None -> Some Dark
            | Some Dark -> Some Light
            | Some Light -> None
      }


let private useStyles = Styles.makeStyles(fun theme ->
  let drawerWidth = 240
  {|
    root = Styles.create [
      style.display.flex
    ]
    appBar = Styles.create [
      style.zIndex (theme.zIndex.drawer + 1)
    ]
    appBarTitle = Styles.create [
      style.flexGrow 1
    ]
    drawer = Styles.create [
      style.width (length.px drawerWidth)
      style.flexShrink 0  // TODO: Does this do anything?
    ]
    drawerPaper = Styles.create [
      style.width (length.px drawerWidth)
    ]
    content = Styles.create [
      style.width 0  // TODO: is there a better way to prevent long code boxes extending past the screen?
      style.flexGrow 1
      style.padding (theme.spacing 3)
    ]
    nestedMenuItem = Styles.create [
      style.paddingLeft (theme.spacing 4)
    ]
    toolbar = Styles.create [
      yield! theme.mixins.toolbarStyles
    ]
  |}
)


module Theme =

  let defaultTheme = Styles.createMuiTheme()

  let light = Styles.createMuiTheme(jsOptions<Theme>(fun t ->
    t.palette <- jsOptions<Palette>(fun p ->
      p.``type`` <- PaletteType.Light
      p.primary <- !^Colors.indigo
      p.secondary <- !^Colors.pink
      p.background <- jsOptions<BackgroundPalette>(fun p ->
        p.``default`` <- "#fff"
      )
    )

    t.typography <- jsOptions<Typography>(fun t ->
      t.h1 <- jsOptions<VariantTypography>(fun vt ->
        vt.fontSize <- "3rem"
      )
      t.h2 <- jsOptions<VariantTypography>(fun vt ->
        vt.fontSize <- "2rem"
      )
      t.h3 <- jsOptions<VariantTypography>(fun vt ->
        vt.fontSize <- "1.5rem"
      )
    )
  ))

  let dark = Styles.createMuiTheme(jsOptions<Theme>(fun t ->
    t.palette <- jsOptions<Palette>(fun p ->
      p.``type`` <- PaletteType.Dark
      p.primary <- !^Colors.lightBlue
      p.secondary <- !^Colors.pink
      p.background <- jsOptions<BackgroundPalette>(fun p ->
        p.``default`` <- defaultTheme.palette.grey.``900``
      )
    )

    t.typography <- jsOptions<Typography>(fun t ->
      t.h1 <- jsOptions<VariantTypography>(fun vt ->
        vt.fontSize <- "3rem"
      )
      t.h2 <- jsOptions<VariantTypography>(fun vt ->
        vt.fontSize <- "2rem"
      )
      t.h3 <- jsOptions<VariantTypography>(fun vt ->
        vt.fontSize <- "1.5rem"
      )
    )

    t.setOverrides [
      overrides.muiAppBar [
        overrides.muiAppBar.colorDefault [
          style.backgroundColor defaultTheme.palette.grey.A400
        ]
      ]
      overrides.muiPaper [
        overrides.muiPaper.root [
          style.backgroundColor defaultTheme.palette.grey.A400
        ]
      ]
      overrides.muiDrawer [
        overrides.muiDrawer.paper [
          style.backgroundColor defaultTheme.palette.grey.``900``
        ]
      ]
    ]

    t.setProps [
      themeProps.muiAppBar [
        appBar.color.default'
      ]
    ]
  ))


let toolbar model dispatch =
  let c = useStyles ()
  Mui.toolbar [
    Mui.typography [
      typography.variant.h6
      typography.color.inherit'
      typography.children "Feliz.MaterialUI"
      prop.className c.appBarTitle
    ]

    // Light/dark mode button
    Mui.tooltip [
      tooltip.title(
        match model.CustomThemeMode with
        | None -> "Using system light/dark theme"
        | Some Light -> "Using light theme"
        | Some Dark -> "Using dark theme"
      )
      tooltip.children(
        Mui.iconButton [
          prop.onClick (fun _ -> dispatch ToggleCustomThemeMode)
          iconButton.color.inherit'
          iconButton.children [
            match model.CustomThemeMode with
            | None -> brightnessAutoIcon []
            | Some Light -> brightness7Icon []
            | Some Dark -> brightness4Icon []
          ]
        ]
      )
    ]

    // GitHub button
    Mui.tooltip [
      tooltip.title "Feliz.MaterialUI on GitHub"
      tooltip.children(
        Mui.iconButton [
          prop.href "https://github.com/cmeeren/Feliz.MaterialUI"
          iconButton.component' "a"
          iconButton.color.inherit'
          iconButton.children (gitHubIcon [])
        ]
      )
    ]
  ]


let menuContainer = React.functionComponent(fun (name: string, pathPrefix: string, currentPath: string list, children: seq<ReactElement>) ->
  let isInPath =
    match currentPath with
    | hd::_ when hd = pathPrefix -> true
    | _ -> false
  let isOpen, setIsOpen = React.useState true
  React.fragment [
    Mui.listItem [
      prop.onClick (fun _ -> setIsOpen (not isOpen))
      listItem.button true
      listItem.children [
        Mui.listItemText name
      ]
    ]
    Mui.collapse [
      collapse.in' (isInPath || isOpen)
      collapse.children [
        Mui.list [
          list.disablePadding true
          list.children children
        ]
      ]
    ]
  ]
)


let drawer model dispatch =
  let c = useStyles ()

  let menuItem isNested (name: string) path =
    let fragment = "#" + (path |> String.concat "/")
    Mui.listItem [
      prop.key fragment
      prop.href fragment
      if isNested then
        prop.className c.nestedMenuItem
      listItem.button true
      listItem.component' "a"
      listItem.selected ((model.CurrentPath = path))
      listItem.children [
        Mui.listItemText name
      ]
    ]

  Mui.drawer [
    prop.className c.drawer
    drawer.variant.permanent
    drawer.classes [
      classes.drawer.paper c.drawerPaper
    ]
    drawer.children [
      Html.div [ prop.className c.toolbar ]
      Mui.list [
        list.component' "nav"
        list.children [
          menuItem false "Home" []
          menuContainer ("Usage", Url.usage, model.CurrentPath, [
            menuItem true "Installation" [Url.usage; Url.installation]
            menuItem true "Components/props" [Url.usage; Url.components_props]
            menuItem true "Classes" [Url.usage; Url.classes]
            menuItem true "Styling using makeStyles" [Url.usage; Url.styling]
            menuItem true "Styling using themes" [Url.usage; Url.themes]
            menuItem true "Other hooks" [Url.usage; Url.hooks]
          ])
          menuItem false "Ecosystem" [Url.ecosystem]
          menuContainer ("Components", Url.components, model.CurrentPath, [
            menuItem true "Autocomplete" [Url.components; Url.autocomplete]
          ])
        ]
      ]
    ]
  ]


let App = FunctionComponent.Of((fun (model, dispatch) ->
  let isDarkMode = Hooks.useMediaQuery "@media (prefers-color-scheme: dark)"
  let systemThemeMode = if isDarkMode then Dark else Light
  let c = useStyles ()
  Router.router [
    Router.onUrlChanged (SetPath >> dispatch)
    Router.application [
      Mui.themeProvider [
        themeProvider.theme (
          match model.CustomThemeMode |> Option.defaultValue systemThemeMode with
          | Dark -> Theme.dark
          | Light -> Theme.light
        )
        themeProvider.children [
          Mui.cssBaseline []
          Html.div [
            prop.className c.root
            prop.children [
              Mui.cssBaseline []
              Mui.appBar [
                prop.className c.appBar
                appBar.position.fixed'
                appBar.children [
                  toolbar model dispatch
                ]
              ]
              drawer model dispatch
              Html.main [
                prop.className c.content
                prop.children [
                  Html.div [ prop.className c.toolbar ]
                  markdownViewer {| path = (Url.pages :: model.CurrentPath @ [Url.indexMd]) |}
                ]
              ]
            ]
          ]
        ]
      ]
    ]
  ]
), "App", memoEqualsButFunctions)

let view model dispatch =
  App (model, dispatch)