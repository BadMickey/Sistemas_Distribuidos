# Sistemas_Distribuidos
Repositório destinado a disciplina de Sistemas Distribuidos na IFG - Inhumas.

Aqui será trabalhado usando um projeto de Blockchain de POOII como base, buscaremos implementar essa blockchain operando em duas máquinas diferentes para idealizar o conceito de nós para validação, dessa forma iremos desenvolver um projeto que irá rodar em duas máquinas diferentes comunicando entre si.

Att 03/12/2023: Consegui programar a Blockchain de forma que funcione em qualquer máquina e com quantos nós quisermos desde que os IP's sejam corretamente configurados assim como os bancos físicos de cada máquina, os nós conta com um nó servidor(uma espécie de distribuidor) que valida os blocos e propagam para todos os nós clientes conectados, blocos estes que podem ser disparados pela API, pelo próprio nó servidor e até mesmo pelos nós clientes. O nó servidor conta com diversas funções: de validação do bloco, verificação da blockchain, propagação dos blocos, compartilhamento da blockchain para os nós clientes que se conectam e com todas as ações necessárias seja de atualizar somente o banco físico do nó cliente ou verificar se tem algum bloco incorreto.
